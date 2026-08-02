using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.Models;

public class Location
{
    public int Id { get; set; }
    [MaxLength(30)] public string? Code { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(180)] public string? Address { get; set; }
    [MaxLength(80)] public string? District { get; set; }
    [MaxLength(80)] public string? Province { get; set; }
    [MaxLength(80)] public string? Department { get; set; }
    [MaxLength(30)] public string? ContactPhone { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SupervisorLocation> Supervisors { get; set; } = [];
}

public class SupervisorLocation
{
    public int SupervisorId { get; set; }
    public AppUser Supervisor { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Position
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
