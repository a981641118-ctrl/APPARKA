using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.Models;

public class AppUser
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Role { get; set; } = AppRoles.Collaborator;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    [MaxLength(80)] public string? ActivationToken { get; set; }
    public DateTimeOffset? ActivationExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class AppRoles
{
    public const string Administrator = "ADMINISTRATOR";
    public const string Supervisor = "SUPERVISOR";
    public const string Hr = "HR";
    public const string Collaborator = "COLLABORATOR";
    public static readonly string[] All = [Administrator, Supervisor, Hr, Collaborator];
}
