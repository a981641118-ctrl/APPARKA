using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(
    AppDbContext db,
    AuditService audit,
    CurrentUserService current,
    PeruClock clock,
    IOptions<TrainingOptions> trainingOptions) : Controller
{
    private readonly TrainingOptions options = trainingOptions.Value;

    public async Task<IActionResult> Index(string? search, string? role, bool? isActive, int page = 1)
    {
        search = search?.Trim() ?? string.Empty;
        role = role is not null && AppRoles.All.Contains(role) ? role : string.Empty;
        var usersQuery = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            usersQuery = usersQuery.Where(x => EF.Functions.ILike(x.FullName, pattern)
                || EF.Functions.ILike(x.Email, pattern)
                || (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, pattern)));
        }
        if (!string.IsNullOrWhiteSpace(role)) usersQuery = usersQuery.Where(x => x.Role == role);
        if (isActive is not null) usersQuery = usersQuery.Where(x => x.IsActive == isActive.Value);

        var users = await usersQuery.OrderBy(x => x.Role).ThenBy(x => x.FullName)
            .ToPagedResultAsync(page);
        var repeatedAlerts = await db.AuditLogs.AsNoTracking()
            .Where(x => x.Action == "REPEATED_SUPERVISOR_OBSERVATION")
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();
        return View(new AdminIndexViewModel
        {
            Users = users,
            RepeatedObservationAlerts = repeatedAlerts,
            UserCount = await db.Users.CountAsync(),
            AssignmentCount = await db.TrainingAssignments.CountAsync(),
            CompletedCount = await db.ActivityEvidences.CountAsync(x => x.CompletedAt != null),
            WarningCount = await db.AuditLogs.CountAsync(x => x.Severity != AuditSeverity.Info),
            Search = search,
            Role = role,
            IsActive = isActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockActivity(ExceptionalActivityUnlockViewModel model)
    {
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment)
            .Include(x => x.Template)
            .FirstOrDefaultAsync(x => x.Id == model.EvidenceId && x.AssignmentId == model.AssignmentId);
        if (evidence is null) return NotFound();

        if (evidence.Status is not (EvidenceStatus.Scheduled or EvidenceStatus.Expired))
            return BackToDetails(model.AssignmentId, "La actividad ya no está bloqueada y no necesita una activación excepcional.");
        if (model.IsSimulation is null)
            return BackToDetails(model.AssignmentId, "Indica si la activación corresponde a una simulación.");

        var newDueAt = ToUtc(model.NewDueAt);
        if (newDueAt <= clock.UtcNow.AddMinutes(5))
            return BackToDetails(model.AssignmentId, "La nueva fecha límite debe ser posterior a la hora actual.");

        var reason = model.Reason?.Trim();
        if (!model.IsSimulation.Value && (string.IsNullOrWhiteSpace(reason) || reason.Length < 15))
            return BackToDetails(model.AssignmentId, "Describe el motivo real de la activación en al menos 15 caracteres.");

        var previousStatus = evidence.Status;
        evidence.OriginalDueAt ??= evidence.DueAt;
        evidence.AvailableFrom = clock.UtcNow;
        evidence.DueAt = newDueAt;
        evidence.Status = EvidenceStatus.Available;
        evidence.WasExceptionallyUnlocked = true;
        evidence.ExceptionalUnlockIsSimulation = model.IsSimulation.Value;
        evidence.ExceptionallyUnlockedAt = clock.UtcNow;
        evidence.ExceptionallyUnlockedById = current.UserId;
        evidence.ExceptionalUnlockReason = model.IsSimulation.Value ? null : reason;
        if (!model.IsSimulation.Value)
            evidence.Assignment.HasRealExceptionalAccess = true;

        await db.SaveChangesAsync();
        await audit.WriteAsync(
            model.IsSimulation.Value ? "SIMULATION_ACTIVITY_UNLOCKED" : "EXCEPTIONAL_ACTIVITY_UNLOCKED",
            model.IsSimulation.Value ? "AdministrativeSimulation" : nameof(ActivityEvidence),
            evidence.Id,
            model.IsSimulation.Value
                ? $"Simulación administrativa: se habilitó la actividad {evidence.Sequence} hasta el {clock.Format(newDueAt)}. Estado anterior: {previousStatus.Label()}."
                : $"Actividad {evidence.Sequence} habilitada excepcionalmente hasta el {clock.Format(newDueAt)}. Estado anterior: {previousStatus.Label()}. Motivo: {reason}",
            model.IsSimulation.Value ? AuditSeverity.Info : AuditSeverity.Warning);

        TempData["Success"] = model.IsSimulation.Value
            ? "La actividad fue habilitada para la simulación hasta la fecha seleccionada."
            : "La actividad fue habilitada y el caso quedó registrado en la trazabilidad.";
        return RedirectToAction("Details", "Hr", new { id = model.AssignmentId }, "cronograma");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockFinalExam(ExceptionalFinalExamUnlockViewModel model)
    {
        var assignment = await db.TrainingAssignments
            .Include(x => x.FinalExamAttempts)
            .FirstOrDefaultAsync(x => x.Id == model.AssignmentId);
        if (assignment is null) return NotFound();

        if (assignment.Status is TrainingStatus.ReadyForFinalExam or TrainingStatus.Apt
            or TrainingStatus.AptObserved or TrainingStatus.NotApt or TrainingStatus.Cancelled)
            return BackToDetails(model.AssignmentId, "El examen final ya no está bloqueado o el proceso ya terminó.");
        if (assignment.FinalExamAttempts.Count >= options.FinalExamMaxAttempts)
            return BackToDetails(model.AssignmentId, "Ya se utilizaron todos los intentos permitidos del examen final.");
        if (model.IsSimulation is null)
            return BackToDetails(model.AssignmentId, "Indica si la activación corresponde a una simulación.");

        var newDueAt = ToUtc(model.NewDueAt);
        if (newDueAt <= clock.UtcNow.AddMinutes(5))
            return BackToDetails(model.AssignmentId, "La nueva fecha límite debe ser posterior a la hora actual.");

        var reason = model.Reason?.Trim();
        if (!model.IsSimulation.Value && (string.IsNullOrWhiteSpace(reason) || reason.Length < 15))
            return BackToDetails(model.AssignmentId, "Describe el motivo real de la activación en al menos 15 caracteres.");

        assignment.FinalExamExceptionalAccess = true;
        assignment.FinalExamExceptionalAccessIsSimulation = model.IsSimulation.Value;
        assignment.FinalExamExceptionalAccessGrantedAt = clock.UtcNow;
        assignment.FinalExamExceptionalAccessExpiresAt = newDueAt;
        assignment.FinalExamExceptionalAccessGrantedById = current.UserId;
        assignment.FinalExamExceptionalAccessReason = model.IsSimulation.Value ? null : reason;
        if (!model.IsSimulation.Value)
            assignment.HasRealExceptionalAccess = true;

        await db.SaveChangesAsync();
        await audit.WriteAsync(
            model.IsSimulation.Value ? "SIMULATION_FINAL_EXAM_UNLOCKED" : "EXCEPTIONAL_FINAL_EXAM_UNLOCKED",
            model.IsSimulation.Value ? "AdministrativeSimulation" : nameof(TrainingAssignment),
            assignment.Id,
            model.IsSimulation.Value
                ? $"Simulación administrativa: se habilitó el examen final hasta el {clock.Format(newDueAt)}."
                : $"Examen final habilitado excepcionalmente hasta el {clock.Format(newDueAt)}. Motivo: {reason}",
            model.IsSimulation.Value ? AuditSeverity.Info : AuditSeverity.Warning);

        TempData["Success"] = model.IsSimulation.Value
            ? "El examen final fue habilitado para la simulación hasta la fecha seleccionada."
            : "El examen final fue habilitado y el caso quedó registrado en la trazabilidad.";
        return RedirectToAction("Details", "Hr", new { id = model.AssignmentId }, "examen-final");
    }

    private DateTimeOffset ToUtc(DateTime localDateTime) => clock.At(
        DateOnly.FromDateTime(localDateTime),
        TimeOnly.FromDateTime(localDateTime));

    private IActionResult BackToDetails(int assignmentId, string error)
    {
        TempData["Error"] = error;
        return RedirectToAction("Details", "Hr", new { id = assignmentId });
    }
}
