using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController(AppDbContext db) : Controller
{
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
}
