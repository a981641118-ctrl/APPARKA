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
    public async Task<IActionResult> Dashboard()
    {
        var query = db.TrainingAssignments
            .Include(x => x.Collaborator).Include(x => x.Location).Include(x => x.Position)
            .Include(x => x.Activities).ThenInclude(x => x.Template)
            .AsQueryable();
        if (!User.IsInRole(AppRoles.Administrator)) query = query.Where(x => x.SupervisorId == current.UserId);
        var assignments = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        foreach (var item in assignments) await schedule.RefreshAssignmentStatusAsync(item);
        return View(assignments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCode(int evidenceId)
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
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
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
            return RedirectToAction(nameof(Review), new { id = model.EvidenceId });
        }
        var safeRubric = canonical.Select((item, index) =>
            (item.Criterion, item.IsCritical, model.Items[index].Rating, model.Items[index].Observation)).ToList();
        var result = await workflow.SubmitSupervisorReviewAsync(
            model.EvidenceId,
            current.UserId!.Value,
            safeRubric,
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
        return RedirectToAction(nameof(Dashboard));
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
