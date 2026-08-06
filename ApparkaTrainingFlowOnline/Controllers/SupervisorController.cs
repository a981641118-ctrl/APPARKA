using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "SupervisorOrAdmin")]
public class SupervisorController(
    AppDbContext db,
    CurrentUserService current,
    TrainingWorkflowService workflow,
    TrainingScheduleService schedule,
    PeruClock clock) : Controller
{
    public async Task<IActionResult> Dashboard(
        int? supervisorId = null,
        string? search = null,
        TrainingStatus? status = null,
        int? locationId = null,
        int periodPage = 1,
        int pendingPage = 1,
        int startablePage = 1)
    {
        var isAdministrator = User.IsInRole(AppRoles.Administrator);
        var supervisors = isAdministrator
            ? await db.Users.AsNoTracking()
                .Where(x => x.Role == AppRoles.Supervisor)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.FullName)
                .ToListAsync()
            : [];

        if (isAdministrator && supervisorId is not null && supervisors.All(x => x.Id != supervisorId))
            return NotFound();

        var selectedSupervisorId = isAdministrator ? supervisorId : current.UserId;
        await schedule.RefreshOperationalStatusesAsync(selectedSupervisorId);
        search = search?.Trim() ?? string.Empty;

        var scopedAssignments = db.TrainingAssignments.AsNoTracking().AsQueryable();
        if (selectedSupervisorId is not null)
            scopedAssignments = scopedAssignments.Where(x => x.SupervisorId == selectedSupervisorId.Value);

        var filteredAssignments = scopedAssignments;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            filteredAssignments = filteredAssignments.Where(x => EF.Functions.ILike(x.Collaborator.FullName, pattern)
                || EF.Functions.ILike(x.Collaborator.Email, pattern)
                || (x.Collaborator.EmployeeCode != null && EF.Functions.ILike(x.Collaborator.EmployeeCode, pattern)));
        }
        if (status is not null) filteredAssignments = filteredAssignments.Where(x => x.Status == status.Value);
        if (locationId is not null) filteredAssignments = filteredAssignments.Where(x => x.LocationId == locationId.Value);

        var assignmentPage = await filteredAssignments
            .Include(x => x.Collaborator).Include(x => x.Supervisor)
            .Include(x => x.Location).Include(x => x.Position)
            .Include(x => x.Activities).ThenInclude(x => x.Template)
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedResultAsync(periodPage);

        var filteredIds = filteredAssignments.Select(x => x.Id);
        var actionQuery = db.ActivityEvidences.AsNoTracking()
            .Include(x => x.Assignment).ThenInclude(x => x.Collaborator)
            .Include(x => x.Assignment).ThenInclude(x => x.Supervisor)
            .Include(x => x.Assignment).ThenInclude(x => x.Location)
            .Include(x => x.Template)
            .Where(x => filteredIds.Contains(x.AssignmentId));

        var pending = await actionQuery
            .Where(x => x.Status == EvidenceStatus.AwaitingSupervisor)
            .OrderBy(x => x.DueAt)
            .ToPagedResultAsync(pendingPage, ListPageSizes.Actions);
        var startable = await actionQuery
            .Where(x => x.Status == EvidenceStatus.Available)
            .OrderBy(x => x.DueAt)
            .ToPagedResultAsync(startablePage, ListPageSizes.Actions);

        var locationQuery = db.Locations.AsNoTracking().Where(x => x.IsActive);
        if (selectedSupervisorId is not null)
            locationQuery = locationQuery.Where(x => x.Supervisors.Any(y => y.SupervisorId == selectedSupervisorId.Value));

        return View(new SupervisorDashboardViewModel
        {
            Assignments = assignmentPage,
            PendingActions = pending.Map(x => new SupervisorActionRowViewModel { Assignment = x.Assignment, Evidence = x }),
            StartableActions = startable.Map(x => new SupervisorActionRowViewModel { Assignment = x.Assignment, Evidence = x }),
            Supervisors = supervisors,
            Locations = await locationQuery.OrderBy(x => x.Name).ToListAsync(),
            SelectedSupervisorId = selectedSupervisorId,
            IsAdministrator = isAdministrator,
            ActiveAssignmentCount = await scopedAssignments.CountAsync(x => x.Status == TrainingStatus.Preboarding
                || x.Status == TrainingStatus.InTraining
                || x.Status == TrainingStatus.ReadyForFinalExam),
            Search = search,
            Status = status,
            LocationId = locationId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCode(int evidenceId, int? supervisorId = null)
    {
        var result = await workflow.GenerateValidationCodeAsync(evidenceId, current.UserId!.Value);
        TempData[result.Result.Success ? "Success" : "Error"] = result.Result.Message;
        if (result.Code is not null)
        {
            TempData["ValidationCode"] = result.Code;
            TempData["ValidationCodeEvidenceId"] = evidenceId;
            TempData["ValidationCodeExpiresAt"] = result.ExpiresAt is null
                ? string.Empty
                : clock.Format(result.ExpiresAt.Value, "dd/MM/yyyy HH:mm:ss");
        }
        return RedirectToAction(nameof(Dashboard), new { supervisorId });
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id, int? supervisorId = null)
    {
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment).ThenInclude(x => x.Collaborator)
            .Include(x => x.Template)
            .Include(x => x.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (evidence is null) return NotFound();
        if (!User.IsInRole(AppRoles.Administrator) && evidence.Assignment.SupervisorId != current.UserId) return Forbid();
        var model = new SupervisorReviewViewModel
        {
            EvidenceId = evidence.Id,
            DashboardSupervisorId = supervisorId,
            Evidence = evidence,
            Items = DefaultRubric()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(SupervisorReviewViewModel model)
    {
        var canonical = DefaultRubric();
        if (model.Items.Count != canonical.Count)
        {
            TempData["Error"] = "La rúbrica recibida no es válida.";
            return RedirectToAction(nameof(Review), new { id = model.EvidenceId, supervisorId = model.DashboardSupervisorId });
        }
        var safeRubric = canonical.Select((item, index) =>
            (item.Criterion, item.IsCritical, model.Items[index].Rating,
                model.Items[index].Observation, model.Items[index].GuidanceProvided)).ToList();
        var result = await workflow.SubmitSupervisorReviewAsync(
            model.EvidenceId,
            current.UserId!.Value,
            safeRubric,
            model.OverallAssessment,
            model.ObservedStrength,
            model.OverallEvidence,
            model.MainImprovement,
            model.Feedback ?? string.Empty);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (!result.Success)
        {
            model.Evidence = await db.ActivityEvidences
                .Include(x => x.Assignment).ThenInclude(x => x.Collaborator)
                .Include(x => x.Template)
                .Include(x => x.Answers).ThenInclude(x => x.Question)
                .FirstAsync(x => x.Id == model.EvidenceId);
            return View(model);
        }
        return RedirectToAction(nameof(Dashboard), new { supervisorId = model.DashboardSupervisorId });
    }

    private static List<RubricInput> DefaultRubric() =>
    [
        new() { Criterion = "Sigue la secuencia del procedimiento", IsCritical = true },
        new() { Criterion = "Aplica los controles de seguridad", IsCritical = true },
        new() { Criterion = "Utiliza correctamente el sistema o equipo" },
        new() { Criterion = "Mantiene una atención adecuada" },
        new() { Criterion = "Verifica y registra el resultado" }
    ];
}
