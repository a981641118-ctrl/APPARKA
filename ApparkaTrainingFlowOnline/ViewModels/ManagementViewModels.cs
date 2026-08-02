using ApparkaTrainingFlowOnline.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.ViewModels;

public class CreateSupervisorViewModel
{
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [MaxLength(30)] public string? EmployeeCode { get; set; }
    [Phone, MaxLength(30)] public string? Phone { get; set; }
    [MinLength(1, ErrorMessage = "Selecciona al menos una sede.")]
    public List<int> LocationIds { get; set; } = [];
    public List<SelectListItem> Locations { get; set; } = [];
}

public class PeopleIndexViewModel
{
    public IReadOnlyList<PersonRowViewModel> Supervisors { get; set; } = [];
    public IReadOnlyList<PersonRowViewModel> Collaborators { get; set; } = [];
}

public class PersonRowViewModel
{
    public AppUser User { get; set; } = null!;
    public string Detail { get; set; } = string.Empty;
}

public class EditPersonViewModel
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [MaxLength(30)] public string? EmployeeCode { get; set; }
    [Phone, MaxLength(30)] public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> LocationIds { get; set; } = [];
    public int? AssignmentId { get; set; }
    public int? LocationId { get; set; }
    public int? SupervisorId { get; set; }
    public List<SelectListItem> Locations { get; set; } = [];
    public List<SelectListItem> Supervisors { get; set; } = [];
}

public class LocationFormViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(180)] public string Address { get; set; } = string.Empty;
    [MaxLength(80)] public string? District { get; set; }
    [MaxLength(80)] public string? Province { get; set; }
    [MaxLength(80)] public string? Department { get; set; }
    [Phone, MaxLength(30)] public string? ContactPhone { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LocationRowViewModel
{
    public Location Location { get; set; } = null!;
    public int SupervisorCount { get; set; }
    public int AssignmentCount { get; set; }
}
