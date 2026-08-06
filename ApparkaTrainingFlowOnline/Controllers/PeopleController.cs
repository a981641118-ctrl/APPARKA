using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "HrOrAdmin")]
public class PeopleController(
    AppDbContext db,
    PasswordService passwords,
    AuditService audit,
    InvitationEmailService invitationEmail,
    TrainingScheduleService schedule) : Controller
{
    public async Task<IActionResult> Index(
        string? supervisorSearch,
        bool? supervisorIsActive,
        int supervisorPage = 1,
        string? collaboratorSearch = null,
        bool? collaboratorIsActive = null,
        int collaboratorPage = 1)
    {
        supervisorSearch = supervisorSearch?.Trim() ?? string.Empty;
        collaboratorSearch = collaboratorSearch?.Trim() ?? string.Empty;

        var supervisorQuery = db.Users
            .AsNoTracking()
            .Include(x => x.SupervisorLocations).ThenInclude(x => x.Location)
            .Where(x => x.Role == AppRoles.Supervisor)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(supervisorSearch))
        {
            var pattern = $"%{supervisorSearch}%";
            supervisorQuery = supervisorQuery.Where(x => EF.Functions.ILike(x.FullName, pattern)
                || EF.Functions.ILike(x.Email, pattern)
                || (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, pattern)));
        }
        if (supervisorIsActive is not null)
            supervisorQuery = supervisorQuery.Where(x => x.IsActive == supervisorIsActive.Value);
        var supervisors = await supervisorQuery.OrderBy(x => x.FullName)
            .AsSplitQuery()
            .ToPagedResultAsync(supervisorPage);

        var collaboratorQuery = db.Users
            .AsNoTracking()
            .Where(x => x.Role == AppRoles.Collaborator)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(collaboratorSearch))
        {
            var pattern = $"%{collaboratorSearch}%";
            collaboratorQuery = collaboratorQuery.Where(x => EF.Functions.ILike(x.FullName, pattern)
                || EF.Functions.ILike(x.Email, pattern)
                || (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, pattern)));
        }
        if (collaboratorIsActive is not null)
            collaboratorQuery = collaboratorQuery.Where(x => x.IsActive == collaboratorIsActive.Value);
        var collaborators = await collaboratorQuery.OrderBy(x => x.FullName)
            .ToPagedResultAsync(collaboratorPage);

        var collaboratorIds = collaborators.Items.Select(x => x.Id).ToList();
        var assignments = await db.TrainingAssignments
            .AsNoTracking()
            .Include(x => x.Location).Include(x => x.Supervisor)
            .Where(x => collaboratorIds.Contains(x.CollaboratorId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var repeatedObservationAlerts = await db.AuditLogs.AsNoTracking()
            .Where(x => x.Action == "REPEATED_SUPERVISOR_OBSERVATION")
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();
        var supervisorIds = repeatedObservationAlerts
            .Select(x => int.TryParse(x.EntityId, out var id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        var supervisorNames = await db.Users.AsNoTracking()
            .Where(x => supervisorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        return View(new PeopleIndexViewModel
        {
            Supervisors = supervisors.Map(x => new PersonRowViewModel
            {
                User = x,
                Detail = x.SupervisorLocations.Count == 0
                    ? "Sin sedes asignadas"
                    : string.Join(" · ", x.SupervisorLocations.OrderBy(y => y.Location.Name).Select(y => y.Location.Name))
            }),
            Collaborators = collaborators.Map(x =>
            {
                var assignment = assignments.FirstOrDefault(y => y.CollaboratorId == x.Id);
                return new PersonRowViewModel
                {
                    User = x,
                    Detail = assignment is null
                        ? "Sin periodo asignado"
                        : $"{assignment.Location.Name} · Supervisor: {assignment.Supervisor.FullName}"
                };
            }),
            RepeatedObservationAlerts = repeatedObservationAlerts.Select(x =>
            {
                var supervisorId = int.TryParse(x.EntityId, out var id) ? id : 0;
                return new RepeatedObservationAlertViewModel
                {
                    SupervisorName = supervisorNames.GetValueOrDefault(supervisorId, "Supervisor no disponible"),
                    Observation = x.Detail,
                    CreatedAt = x.CreatedAt
                };
            }).ToList(),
            SupervisorSearch = supervisorSearch,
            SupervisorIsActive = supervisorIsActive,
            CollaboratorSearch = collaboratorSearch,
            CollaboratorIsActive = collaboratorIsActive
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateSupervisor()
    {
        var model = new CreateSupervisorViewModel();
        await FillLocationOptions(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupervisor(CreateSupervisorViewModel model)
    {
        var email = NormalizeEmail(model.Email);
        var employeeCode = NormalizeCode(model.EmployeeCode);
        var locationIds = model.LocationIds.Distinct().ToList();
        if (await db.Users.AnyAsync(x => x.Email == email))
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo.");
        if (employeeCode is not null && await db.Users.AnyAsync(x => x.EmployeeCode == employeeCode))
            ModelState.AddModelError(nameof(model.EmployeeCode), "El documento de identidad ya está registrado.");
        if (locationIds.Count == 0)
            ModelState.AddModelError(nameof(model.LocationIds), "Selecciona al menos una sede.");
        if (await db.Locations.CountAsync(x => locationIds.Contains(x.Id) && x.IsActive) != locationIds.Count)
            ModelState.AddModelError(nameof(model.LocationIds), "Una de las sedes seleccionadas no está disponible.");
        if (!ModelState.IsValid)
        {
            await FillLocationOptions(model);
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = email,
            EmployeeCode = employeeCode,
            Phone = model.Phone.Trim(),
            Role = AppRoles.Supervisor,
            ActivationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            ActivationExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            MustChangePassword = true
        };
        user.PasswordHash = passwords.Hash(user, Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
        foreach (var locationId in locationIds)
            user.SupervisorLocations.Add(new SupervisorLocation { LocationId = locationId });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await audit.WriteAsync("SUPERVISOR_CREATED", nameof(AppUser), user.Id,
            $"Supervisor registrado y asignado a {locationIds.Count} sede(s).");

        var activationLink = Url.Action("Activate", "Account", new { token = user.ActivationToken }, Request.Scheme) ?? string.Empty;
        var emailSent = await invitationEmail.SendSupervisorAsync(user.Email, user.FullName, activationLink);
        TempData["ActivationLink"] = activationLink;
        TempData["Success"] = emailSent
            ? "Supervisor registrado e invitación enviada."
            : "Supervisor registrado. Copia el enlace de activación para enviarlo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await EditableUser(id);
        if (user is null) return NotFound();
        var model = new EditPersonViewModel
        {
            Id = user.Id,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            EmployeeCode = user.EmployeeCode ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            IsActive = user.IsActive,
            LocationIds = user.SupervisorLocations.Select(x => x.LocationId).ToList()
        };
        if (user.Role == AppRoles.Collaborator)
        {
            var assignment = await db.TrainingAssignments
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.CollaboratorId == user.Id);
            if (assignment is not null)
            {
                model.AssignmentId = assignment.Id;
                model.LocationId = assignment.LocationId;
                model.SupervisorId = assignment.SupervisorId;
                model.AccessFrom = assignment.AccessFrom.ToDateTime(TimeOnly.MinValue);
                model.StartDate = assignment.StartDate.ToDateTime(TimeOnly.MinValue);
            }
        }
        await FillEditOptions(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPersonViewModel model)
    {
        var user = await EditableUser(model.Id);
        if (user is null) return NotFound();
        model.Role = user.Role;
        var email = NormalizeEmail(model.Email);
        var employeeCode = NormalizeCode(model.EmployeeCode);
        if (await db.Users.AnyAsync(x => x.Id != user.Id && x.Email == email))
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este correo.");
        if (employeeCode is not null && await db.Users.AnyAsync(x => x.Id != user.Id && x.EmployeeCode == employeeCode))
            ModelState.AddModelError(nameof(model.EmployeeCode), "El documento de identidad ya está registrado.");

        TrainingAssignment? assignment = null;
        var selectedLocationIds = model.LocationIds.Distinct().ToList();
        if (user.Role == AppRoles.Supervisor)
        {
            if (selectedLocationIds.Count == 0)
                ModelState.AddModelError(nameof(model.LocationIds), "Selecciona al menos una sede.");
            if (await db.Locations.CountAsync(x => selectedLocationIds.Contains(x.Id) && x.IsActive) != selectedLocationIds.Count)
                ModelState.AddModelError(nameof(model.LocationIds), "Una de las sedes seleccionadas no está disponible.");
            if (!model.IsActive && await HasOpenAssignments(user.Id))
                ModelState.AddModelError(nameof(model.IsActive), "Reasigna primero los periodos activos antes de desactivar al supervisor.");
        }
        else
        {
            assignment = await db.TrainingAssignments
                .Include(x => x.Activities)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x => x.CollaboratorId == user.Id);
            if (assignment is not null)
            {
                model.AssignmentId = assignment.Id;
                if (model.LocationId is null || !await db.Locations.AnyAsync(x => x.Id == model.LocationId && x.IsActive))
                    ModelState.AddModelError(nameof(model.LocationId), "Selecciona una sede activa.");
                if (model.SupervisorId is null || !await db.Users.AnyAsync(x => x.Id == model.SupervisorId && x.Role == AppRoles.Supervisor && x.IsActive))
                    ModelState.AddModelError(nameof(model.SupervisorId), "Selecciona un supervisor activo.");
                else if (model.LocationId is not null && !await db.SupervisorLocations.AnyAsync(x => x.SupervisorId == model.SupervisorId && x.LocationId == model.LocationId))
                    ModelState.AddModelError(nameof(model.SupervisorId), "El supervisor no está asignado a la sede seleccionada.");
                if (model.AccessFrom is null)
                    ModelState.AddModelError(nameof(model.AccessFrom), "Selecciona la fecha de acceso a los materiales.");
                if (model.StartDate is null)
                    ModelState.AddModelError(nameof(model.StartDate), "Selecciona el primer día de trabajo.");
                if (model.AccessFrom is not null && model.StartDate is not null && model.AccessFrom.Value.Date > model.StartDate.Value.Date)
                    ModelState.AddModelError(nameof(model.AccessFrom), "El acceso a los materiales no puede ser posterior al primer día de trabajo.");
            }
        }

        if (!ModelState.IsValid)
        {
            await FillEditOptions(model);
            return View(model);
        }

        var previous = $"Nombre: {user.FullName}; correo: {user.Email}; DNI: {user.EmployeeCode ?? "—"}; teléfono: {user.Phone ?? "—"}; estado: {(user.IsActive ? "Activo" : "Inactivo")}";
        var previousAssignment = assignment is null
            ? null
            : $"Sede: {assignment.LocationId}; supervisor: {assignment.SupervisorId}; acceso: {assignment.AccessFrom:dd/MM/yyyy}; inicio: {assignment.StartDate:dd/MM/yyyy}; fin: {assignment.EndDate:dd/MM/yyyy}";
        user.FullName = model.FullName.Trim();
        user.Email = email;
        user.EmployeeCode = employeeCode;
        user.Phone = NormalizeOptional(model.Phone);
        user.IsActive = model.IsActive;

        if (user.Role == AppRoles.Supervisor)
        {
            var remove = user.SupervisorLocations.Where(x => !selectedLocationIds.Contains(x.LocationId)).ToList();
            db.SupervisorLocations.RemoveRange(remove);
            var existing = user.SupervisorLocations.Select(x => x.LocationId).ToHashSet();
            foreach (var locationId in selectedLocationIds.Where(x => !existing.Contains(x)))
                user.SupervisorLocations.Add(new SupervisorLocation { LocationId = locationId });
        }
        else if (assignment is not null)
        {
            assignment.LocationId = model.LocationId!.Value;
            assignment.SupervisorId = model.SupervisorId!.Value;
            assignment.AccessFrom = DateOnly.FromDateTime(model.AccessFrom!.Value);
            schedule.ReprogramActivities(assignment, DateOnly.FromDateTime(model.StartDate!.Value));
        }

        await db.SaveChangesAsync();
        var currentValues = $"Nombre: {user.FullName}; correo: {user.Email}; DNI: {user.EmployeeCode ?? "—"}; teléfono: {user.Phone ?? "—"}; estado: {(user.IsActive ? "Activo" : "Inactivo")}";
        await audit.WriteAsync("PERSON_UPDATED", nameof(AppUser), user.Id,
            $"Información actualizada. Antes: [{previous}]. Después: [{currentValues}].");
        if (assignment is not null)
        {
            var currentAssignment = $"Sede: {assignment.LocationId}; supervisor: {assignment.SupervisorId}; acceso: {assignment.AccessFrom:dd/MM/yyyy}; inicio: {assignment.StartDate:dd/MM/yyyy}; fin: {assignment.EndDate:dd/MM/yyyy}";
            await audit.WriteAsync("TRAINING_ASSIGNMENT_UPDATED", nameof(TrainingAssignment), assignment.Id,
                $"Programación actualizada. Antes: [{previousAssignment}]. Después: [{currentAssignment}].");
        }
        TempData["Success"] = "Información actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendAccess(int id)
    {
        var user = await EditableUser(id);
        if (user is null) return NotFound();
        if (!user.IsActive)
        {
            TempData["Error"] = "Activa la cuenta antes de generar un nuevo enlace de acceso.";
            return RedirectToAction(nameof(Index));
        }

        string accessLink;
        bool emailSent;
        if (user.MustChangePassword)
        {
            user.ActivationToken = CreateToken(24);
            user.ActivationExpiresAt = DateTimeOffset.UtcNow.AddDays(14);
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAt = null;
            await db.SaveChangesAsync();

            accessLink = Url.Action("Activate", "Account", new { token = user.ActivationToken }, Request.Scheme) ?? string.Empty;
            if (user.Role == AppRoles.Collaborator)
            {
                var assignment = await db.TrainingAssignments.AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.CollaboratorId == user.Id);
                emailSent = assignment is not null
                    ? await invitationEmail.SendAsync(user.Email, user.FullName, accessLink, assignment.AccessFrom, assignment.StartDate)
                    : await invitationEmail.SendSupervisorAsync(user.Email, user.FullName, accessLink);
            }
            else
            {
                emailSent = await invitationEmail.SendSupervisorAsync(user.Email, user.FullName, accessLink);
            }
        }
        else
        {
            var token = CreateToken(32);
            user.PasswordResetTokenHash = HashToken(token);
            user.PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            await db.SaveChangesAsync();

            accessLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme) ?? string.Empty;
            emailSent = await invitationEmail.SendPasswordResetAsync(user.Email, user.FullName, accessLink);
        }

        await audit.WriteAsync("ACCESS_LINK_REISSUED", nameof(AppUser), user.Id,
            "RR. HH. o Administración generó un nuevo enlace personal de acceso.");
        TempData["ActivationLink"] = accessLink;
        TempData["Success"] = emailSent
            ? "El nuevo enlace fue enviado al correo registrado."
            : "El enlace fue generado. Cópialo y envíalo de forma privada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Locations(string? search, bool? isActive, int page = 1)
    {
        search = search?.Trim() ?? string.Empty;
        var query = db.Locations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern)
                || (x.Code != null && EF.Functions.ILike(x.Code, pattern))
                || (x.Address != null && EF.Functions.ILike(x.Address, pattern))
                || (x.District != null && EF.Functions.ILike(x.District, pattern)));
        }
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);

        var rows = await query.OrderBy(x => x.Name)
            .Select(location => new LocationRowViewModel
            {
                Location = location,
                SupervisorCount = location.Supervisors.Count,
                AssignmentCount = db.TrainingAssignments.Count(x => x.LocationId == location.Id)
            })
            .ToPagedResultAsync(page);
        return View(new LocationsIndexViewModel
        {
            Locations = rows,
            Search = search,
            IsActive = isActive
        });
    }

    [HttpGet]
    public IActionResult CreateLocation() => View("LocationForm", new LocationFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLocation(LocationFormViewModel model)
    {
        var code = NormalizeRequiredCode(model.Code);
        if (await db.Locations.AnyAsync(x => x.Code == code))
            ModelState.AddModelError(nameof(model.Code), "Ya existe una sede con este código.");
        if (!ModelState.IsValid) return View("LocationForm", model);
        var location = new Location();
        MapLocation(location, model, code);
        db.Locations.Add(location);
        await db.SaveChangesAsync();
        await audit.WriteAsync("LOCATION_CREATED", nameof(Location), location.Id,
            $"Sede {location.Code} - {location.Name} registrada.");
        TempData["Success"] = "Sede registrada correctamente.";
        return RedirectToAction(nameof(Locations));
    }

    [HttpGet]
    public async Task<IActionResult> EditLocation(int id)
    {
        var location = await db.Locations.FindAsync(id);
        if (location is null) return NotFound();
        return View("LocationForm", new LocationFormViewModel
        {
            Id = location.Id, Code = location.Code ?? string.Empty, Name = location.Name,
            Address = location.Address ?? string.Empty, District = location.District,
            Province = location.Province, Department = location.Department,
            ContactPhone = location.ContactPhone, Notes = location.Notes, IsActive = location.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLocation(LocationFormViewModel model)
    {
        var location = await db.Locations.FindAsync(model.Id);
        if (location is null) return NotFound();
        var code = NormalizeRequiredCode(model.Code);
        if (await db.Locations.AnyAsync(x => x.Id != location.Id && x.Code == code))
            ModelState.AddModelError(nameof(model.Code), "Ya existe una sede con este código.");
        if (!ModelState.IsValid) return View("LocationForm", model);
        var previous = $"{location.Code} - {location.Name} - {location.Address} - {(location.IsActive ? "Activa" : "Inactiva")}";
        MapLocation(location, model, code);
        await db.SaveChangesAsync();
        await audit.WriteAsync("LOCATION_UPDATED", nameof(Location), location.Id,
            $"Sede actualizada. Antes: [{previous}]. Después: [{location.Code} - {location.Name} - {location.Address} - {(location.IsActive ? "Activa" : "Inactiva")}].");
        TempData["Success"] = "Sede actualizada correctamente.";
        return RedirectToAction(nameof(Locations));
    }

    private async Task<AppUser?> EditableUser(int id) => await db.Users
        .Include(x => x.SupervisorLocations)
        .FirstOrDefaultAsync(x => x.Id == id && (x.Role == AppRoles.Supervisor || x.Role == AppRoles.Collaborator));

    private Task<bool> HasOpenAssignments(int supervisorId) => db.TrainingAssignments.AnyAsync(x =>
        x.SupervisorId == supervisorId && x.Status != TrainingStatus.Apt
        && x.Status != TrainingStatus.NotApt && x.Status != TrainingStatus.Cancelled);

    private async Task FillLocationOptions(CreateSupervisorViewModel model) =>
        model.Locations = await ActiveLocationOptions(model.LocationIds);

    private async Task FillEditOptions(EditPersonViewModel model)
    {
        model.Locations = await ActiveLocationOptions(model.LocationIds);
        var supervisors = await db.Users
            .AsNoTracking()
            .Include(x => x.SupervisorLocations)
            .Where(x => x.Role == AppRoles.Supervisor && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync();
        model.Supervisors = supervisors
            .Select(x => new SelectListItem(x.FullName, x.Id.ToString(), model.SupervisorId == x.Id))
            .ToList();
        model.SupervisorLocationIds = supervisors.ToDictionary(
            x => x.Id,
            x => x.SupervisorLocations.Select(y => y.LocationId).OrderBy(y => y).ToArray());
    }

    private async Task<List<SelectListItem>> ActiveLocationOptions(IEnumerable<int> selected)
    {
        var selectedIds = selected.ToHashSet();
        return await db.Locations.Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedIds.Contains(x.Id)))
            .ToListAsync();
    }

    private static void MapLocation(Location location, LocationFormViewModel model, string code)
    {
        location.Code = code;
        location.Name = model.Name.Trim();
        location.Address = model.Address.Trim();
        location.District = NormalizeOptional(model.District);
        location.Province = NormalizeOptional(model.Province);
        location.Department = NormalizeOptional(model.Department);
        location.ContactPhone = NormalizeOptional(model.ContactPhone);
        location.Notes = NormalizeOptional(model.Notes);
        location.IsActive = model.IsActive;
    }

    private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();
    private static string? NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string NormalizeRequiredCode(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string CreateToken(int byteCount) => Convert.ToHexString(RandomNumberGenerator.GetBytes(byteCount)).ToLowerInvariant();
    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
