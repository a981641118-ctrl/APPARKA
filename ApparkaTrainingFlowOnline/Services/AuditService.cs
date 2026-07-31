using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;

namespace ApparkaTrainingFlowOnline.Services;

public class AuditService(AppDbContext db, CurrentUserService current)
{
    public async Task WriteAsync(string action, string entityType, object entityId, string detail,
        AuditSeverity severity = AuditSeverity.Info)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = current.UserId,
            Actor = current.Name,
            Role = current.Role,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString() ?? string.Empty,
            Detail = detail,
            Severity = severity,
            IpAddress = current.Ip,
            UserAgent = current.UserAgent
        });
        await db.SaveChangesAsync();
    }
}
