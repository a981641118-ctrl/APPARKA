using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "CollaboratorOnly")]
public class TrainingController(
    AppDbContext db,
    CurrentUserService current,
    TrainingScheduleService schedule,
    TrainingWorkflowService workflow,
    PeruClock clock) : Controller
{
    public async Task<IActionResult> Index()
    {
        var assignment = await db.TrainingAssignments
            .Include(x => x.Collaborator).Include(x => x.Supervisor)
            .Include(x => x.Location).Include(x => x.Position)
            .Include(x => x.Activities).ThenInclude(x => x.Template)
            .Include(x => x.FinalExamAttempts)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.CollaboratorId == current.UserId);
        if (assignment is null) return View("NoAssignment");
        if (clock.Today < assignment.AccessFrom) return View("AccessPending", assignment);
        await schedule.RefreshAssignmentStatusAsync(assignment);
        ViewBag.Materials = await db.LearningMaterials.Where(x => x.PositionId == assignment.PositionId && x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        ViewBag.CompletedMaterials = await db.LearningMaterialProgress.Where(x => x.UserId == current.UserId).Select(x => x.MaterialId).ToListAsync();
        ViewBag.Today = clock.Today;
        return View(assignment);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteMaterial(int materialId)
    {
        var userId = current.UserId!.Value;
        var assignment = await db.TrainingAssignments.FirstOrDefaultAsync(x => x.CollaboratorId == userId);
        if (assignment is null || clock.Today < assignment.AccessFrom) return Forbid();
        var material = await db.LearningMaterials.FirstOrDefaultAsync(x => x.Id == materialId && x.PositionId == assignment.PositionId && x.IsActive);
        if (material is null) return NotFound();
        if (!await db.LearningMaterialProgress.AnyAsync(x => x.UserId == userId && x.MaterialId == materialId))
        {
            db.LearningMaterialProgress.Add(new LearningMaterialProgress { UserId = userId, MaterialId = materialId });
            await db.SaveChangesAsync();
        }
        TempData["Success"] = "Material marcado como estudiado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Material(int id)
    {
        var userId = current.UserId!.Value;
        var assignment = await db.TrainingAssignments
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.CollaboratorId == userId);
        if (assignment is null || clock.Today < assignment.AccessFrom) return Forbid();

        var material = await db.LearningMaterials
            .FirstOrDefaultAsync(x => x.Id == id && x.PositionId == assignment.PositionId && x.IsActive);
        if (material is null) return NotFound();

        LearningModuleContent content;
        try
        {
            content = JsonSerializer.Deserialize<LearningModuleContent>(material.ContentJson) ?? new();
        }
        catch (JsonException)
        {
            content = new LearningModuleContent { Introduction = material.Summary };
        }

        return View(new LearningMaterialPageViewModel
        {
            Material = material,
            Content = content,
            IsCompleted = await db.LearningMaterialProgress
                .AnyAsync(x => x.UserId == userId && x.MaterialId == material.Id)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Activity(int id)
    {
        var evidence = await LoadEvidence(id);
        if (evidence is null || evidence.Assignment.CollaboratorId != current.UserId) return NotFound();
        return View(new ActivityPageViewModel
        {
            Evidence = evidence,
            Questions = evidence.QuestionSelections
                .OrderBy(x => x.DisplayOrder)
                .Select(x => QuestionPresentationFactory.Create(x.Question, x.OptionOrder))
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> StartActivity(int evidenceId, string? oneTimeCode)
    {
        var result = await workflow.StartActivityAsync(evidenceId, current.UserId!.Value, oneTimeCode);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Activity), new { id = evidenceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitActivity(int evidenceId, Dictionary<int, string>? answers)
    {
        var result = await workflow.SubmitActivityAnswersAsync(evidenceId, current.UserId!.Value, answers);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Activity), new { id = evidenceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartFinalExam(int assignmentId)
    {
        var result = await workflow.StartFinalExamAsync(assignmentId, current.UserId!.Value);
        TempData[result.Result.Success ? "Success" : "Error"] = result.Result.Message;
        return result.Attempt is null
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(FinalExam), new { attemptId = result.Attempt.Id });
    }

    [HttpGet]
    public async Task<IActionResult> FinalExam(int attemptId)
    {
        var attempt = await db.FinalExamAttempts
            .Include(x => x.Assignment)
            .Include(x => x.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == attemptId && x.Assignment.CollaboratorId == current.UserId);
        if (attempt is null) return NotFound();
        return View(new FinalExamViewModel
        {
            Attempt = attempt,
            Questions = attempt.Answers
                .OrderBy(x => x.DisplayOrder)
                .Select(x => QuestionPresentationFactory.Create(x.Question, x.OptionOrder))
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalExam(int attemptId, Dictionary<int, string>? answers)
    {
        var result = await workflow.SubmitFinalExamAsync(attemptId, current.UserId!.Value, answers);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private Task<ActivityEvidence?> LoadEvidence(int id) => db.ActivityEvidences
        .Include(x => x.Assignment).ThenInclude(x => x.Supervisor)
        .Include(x => x.Template).ThenInclude(x => x.Questions)
        .Include(x => x.QuestionSelections).ThenInclude(x => x.Question)
        .Include(x => x.Answers).ThenInclude(x => x.Question)
        .Include(x => x.Rubric)
        .FirstOrDefaultAsync(x => x.Id == id);
}
