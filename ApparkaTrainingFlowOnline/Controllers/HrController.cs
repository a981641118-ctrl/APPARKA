using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "HrOrAdmin")]
public class HrController(
    AppDbContext db,
    PasswordService passwords,
    TrainingScheduleService schedule,
    CurrentUserService current,
    AuditService audit,
    InvitationEmailService invitationEmail,
    PeruClock clock) : Controller
{
    public async Task<IActionResult> Index(string? search, TrainingStatus? status, int? locationId, int page = 1)
    {
        await schedule.RefreshOperationalStatusesAsync();
        search = search?.Trim() ?? string.Empty;
        var assignmentsQuery = db.TrainingAssignments.AsNoTracking()
            .Include(x => x.Collaborator).Include(x => x.Supervisor)
            .Include(x => x.Location).Include(x => x.Position)
            .Include(x => x.Activities).Include(x => x.FinalExamAttempts)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            assignmentsQuery = assignmentsQuery.Where(x => EF.Functions.ILike(x.Collaborator.FullName, pattern)
                || EF.Functions.ILike(x.Collaborator.Email, pattern)
                || (x.Collaborator.EmployeeCode != null && EF.Functions.ILike(x.Collaborator.EmployeeCode, pattern))
                || EF.Functions.ILike(x.Supervisor.FullName, pattern));
        }
        if (status is not null) assignmentsQuery = assignmentsQuery.Where(x => x.Status == status.Value);
        if (locationId is not null) assignmentsQuery = assignmentsQuery.Where(x => x.LocationId == locationId.Value);

        return View(new HrIndexViewModel
        {
            Assignments = await assignmentsQuery.OrderByDescending(x => x.CreatedAt).ToPagedResultAsync(page),
            Locations = await db.Locations.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(),
            Search = search,
            Status = status,
            LocationId = locationId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var position = await db.Positions.FirstAsync(x => x.IsActive && x.Name == "Anfitrión Red Comercial");
        var model = new CreateCollaboratorViewModel { PositionId = position.Id, AccessFrom = clock.Today.ToDateTime(TimeOnly.MinValue), StartDate = clock.Today.AddDays(3).ToDateTime(TimeOnly.MinValue) };
        await FillLists(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCollaboratorViewModel model)
    {
        var position = await db.Positions.FirstOrDefaultAsync(x => x.IsActive && x.Name == "Anfitrión Red Comercial");
        if (position is null)
            ModelState.AddModelError(string.Empty, "No está configurado el perfil Anfitrión Red Comercial.");
        else
            model.PositionId = position.Id;
        if (model.AccessFrom.Date > model.StartDate.Date)
            ModelState.AddModelError(nameof(model.AccessFrom), "El acceso previo no puede ser posterior al inicio.");
        var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail))
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo.");
        var identityDocument = model.IdentityDocument?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.Users.AnyAsync(x => x.EmployeeCode == identityDocument))
            ModelState.AddModelError(nameof(model.IdentityDocument), "Este documento de identidad ya está registrado.");
        if (!await db.Locations.AnyAsync(x => x.Id == model.LocationId && x.IsActive))
            ModelState.AddModelError(nameof(model.LocationId), "Selecciona una sede activa.");
        if (!await db.SupervisorLocations.AnyAsync(x => x.SupervisorId == model.SupervisorId && x.LocationId == model.LocationId))
            ModelState.AddModelError(nameof(model.SupervisorId), "El supervisor seleccionado no está asignado a esta sede.");
        if (!ModelState.IsValid)
        {
            await FillLists(model);
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            EmployeeCode = identityDocument,
            Phone = model.Phone.Trim(),
            Role = AppRoles.Collaborator,
            ActivationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            ActivationExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            MustChangePassword = true
        };
        user.PasswordHash = passwords.Hash(user, Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var assignment = new TrainingAssignment
        {
            CollaboratorId = user.Id,
            SupervisorId = model.SupervisorId,
            CreatedById = current.UserId!.Value,
            LocationId = model.LocationId,
            PositionId = position!.Id,
            AccessFrom = DateOnly.FromDateTime(model.AccessFrom),
            StartDate = DateOnly.FromDateTime(model.StartDate),
            EndDate = DateOnly.FromDateTime(model.StartDate.AddDays(20)),
            Status = DateOnly.FromDateTime(model.StartDate) > clock.Today ? TrainingStatus.Preboarding : TrainingStatus.InTraining
        };
        await schedule.GenerateActivitiesAsync(assignment);
        db.TrainingAssignments.Add(assignment);
        await db.SaveChangesAsync();
        await audit.WriteAsync("COLLABORATOR_ASSIGNED", nameof(TrainingAssignment), assignment.Id,
            $"Periodo creado automáticamente del {assignment.StartDate:dd/MM/yyyy} al {assignment.EndDate:dd/MM/yyyy}.");

        var activationLink = Url.Action("Activate", "Account", new { token = user.ActivationToken }, Request.Scheme) ?? string.Empty;
        var emailSent = await invitationEmail.SendAsync(user.Email, user.FullName, activationLink, assignment.AccessFrom, assignment.StartDate);
        TempData["ActivationLink"] = activationLink;
        TempData["Success"] = emailSent
            ? "Colaborador registrado, cronograma creado e invitación enviada."
            : "Colaborador registrado y cronograma creado. Copia el enlace de activación para enviarlo.";
        return RedirectToAction(nameof(Details), new { id = assignment.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var assignment = await db.TrainingAssignments
            .Include(x => x.Collaborator).Include(x => x.Supervisor)
            .Include(x => x.Location).Include(x => x.Position)
            .Include(x => x.Activities).ThenInclude(x => x.Template)
            .Include(x => x.Activities).ThenInclude(x => x.Answers).ThenInclude(x => x.Question)
            .Include(x => x.FinalExamAttempts)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (assignment is null) return NotFound();
        await schedule.RefreshAssignmentStatusAsync(assignment);
        var activityIds = assignment.Activities.Select(x => x.Id.ToString()).ToList();
        var assignmentId = assignment.Id.ToString();
        ViewBag.AuditLogs = await db.AuditLogs
            .Where(x => (x.EntityType == nameof(TrainingAssignment) && x.EntityId == assignmentId)
                || (x.EntityType == nameof(ActivityEvidence) && activityIds.Contains(x.EntityId)))
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync();
        return View(assignment);
    }

    private async Task FillLists(CreateCollaboratorViewModel model)
    {
        model.Locations = await db.Locations.Where(x => x.IsActive).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
        var supervisors = await db.Users
            .AsNoTracking()
            .Include(x => x.SupervisorLocations)
            .Where(x => x.Role == AppRoles.Supervisor && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync();
        model.Supervisors = supervisors
            .Select(x => new SelectListItem(x.FullName, x.Id.ToString(), x.Id == model.SupervisorId))
            .ToList();
        model.SupervisorLocationIds = supervisors.ToDictionary(
            x => x.Id,
            x => x.SupervisorLocations.Select(y => y.LocationId).OrderBy(y => y).ToArray());
    }
}
