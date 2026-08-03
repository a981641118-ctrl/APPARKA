using ApparkaTrainingFlowOnline.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount = await db.Users.CountAsync();
        ViewBag.AssignmentCount = await db.TrainingAssignments.CountAsync();
        ViewBag.CompletedCount = await db.ActivityEvidences.CountAsync(x => x.CompletedAt != null);
        ViewBag.WarningCount = await db.AuditLogs.CountAsync(x => x.Severity != Models.AuditSeverity.Info);
        ViewBag.RepeatedObservationAlerts = await db.AuditLogs.AsNoTracking()
            .Where(x => x.Action == "REPEATED_SUPERVISOR_OBSERVATION")
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();
        return View(await db.Users.OrderBy(x => x.Role).ThenBy(x => x.FullName).ToListAsync());
    }
}
