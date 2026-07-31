using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.Models;

public class Location
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(180)] public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Position
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
