using ApparkaTrainingFlowOnline.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.ViewModels;

public class CreateSupervisorViewModel
{
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa el documento de identidad.")]
    [RegularExpression("^[A-Za-z0-9]{8,15}$", ErrorMessage = "Usa entre 8 y 15 letras o números, sin espacios ni guiones.")]
    [MaxLength(15)] public string EmployeeCode { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa un teléfono de contacto.")]
    [Phone(ErrorMessage = "Ingresa un número de teléfono válido.")]
    [MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [MinLength(1, ErrorMessage = "Selecciona al menos una sede.")]
    public List<int> LocationIds { get; set; } = [];
    public List<SelectListItem> Locations { get; set; } = [];
}

public class PeopleIndexViewModel
{
    public PagedResult<PersonRowViewModel> Supervisors { get; set; } = new();
    public PagedResult<PersonRowViewModel> Collaborators { get; set; } = new();
    public IReadOnlyList<RepeatedObservationAlertViewModel> RepeatedObservationAlerts { get; set; } = [];
    public string SupervisorSearch { get; set; } = string.Empty;
    public bool? SupervisorIsActive { get; set; }
    public string CollaboratorSearch { get; set; } = string.Empty;
    public bool? CollaboratorIsActive { get; set; }
}

public class AdminIndexViewModel
{
    public PagedResult<AppUser> Users { get; set; } = new();
    public IReadOnlyList<AuditLog> RepeatedObservationAlerts { get; set; } = [];
    public int UserCount { get; set; }
    public int AssignmentCount { get; set; }
    public int CompletedCount { get; set; }
    public int WarningCount { get; set; }
    public string Search { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}

public class ExceptionalActivityUnlockViewModel
{
    [Required] public int AssignmentId { get; set; }
    [Required] public int EvidenceId { get; set; }
    [Required(ErrorMessage = "Indica si la activación es para una simulación.")]
    public bool? IsSimulation { get; set; }
    [Required(ErrorMessage = "Indica la nueva fecha límite.")]
    [DataType(DataType.DateTime)] public DateTime NewDueAt { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
}

public class ExceptionalFinalExamUnlockViewModel
{
    [Required] public int AssignmentId { get; set; }
    [Required(ErrorMessage = "Indica si la activación es para una simulación.")]
    public bool? IsSimulation { get; set; }
    [Required(ErrorMessage = "Indica la nueva fecha límite.")]
    [DataType(DataType.DateTime)] public DateTime NewDueAt { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
}

public class HrIndexViewModel
{
    public PagedResult<TrainingAssignment> Assignments { get; set; } = new();
    public IReadOnlyList<Location> Locations { get; set; } = [];
    public string Search { get; set; } = string.Empty;
    public TrainingStatus? Status { get; set; }
    public int? LocationId { get; set; }
}

public class LocationsIndexViewModel
{
    public PagedResult<LocationRowViewModel> Locations { get; set; } = new();
    public string Search { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}

public class RepeatedObservationAlertViewModel
{
    public string SupervisorName { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public string Collaborators { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
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
    [Required(ErrorMessage = "Ingresa el documento de identidad.")]
    [RegularExpression("^[A-Za-z0-9]{8,15}$", ErrorMessage = "Usa entre 8 y 15 letras o números, sin espacios ni guiones.")]
    [MaxLength(15)] public string EmployeeCode { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa un teléfono de contacto.")]
    [Phone(ErrorMessage = "Ingresa un número de teléfono válido.")]
    [MaxLength(30)] public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<int> LocationIds { get; set; } = [];
    public int? AssignmentId { get; set; }
    public int? LocationId { get; set; }
    public int? SupervisorId { get; set; }
    [DataType(DataType.Date)] public DateTime? AccessFrom { get; set; }
    [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    public List<SelectListItem> Locations { get; set; } = [];
    public List<SelectListItem> Supervisors { get; set; } = [];
    public Dictionary<int, int[]> SupervisorLocationIds { get; set; } = [];
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
