using System.Security.Claims;

namespace ApparkaTrainingFlowOnline.Services;

public class CurrentUserService(IHttpContextAccessor accessor)
{
    public int? UserId => int.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string Name => accessor.HttpContext?.User.Identity?.Name ?? "Sistema";
    public string Role => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public string Ip => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    public string UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown";
    public string DeviceId => accessor.HttpContext?.Request.Cookies["ApparkaDeviceId"] ?? "unknown";
}
