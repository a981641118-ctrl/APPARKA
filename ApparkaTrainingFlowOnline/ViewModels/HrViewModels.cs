using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApparkaTrainingFlowOnline.ViewModels;

public class CreateCollaboratorViewModel
{
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresa el documento de identidad.")]
    [RegularExpression("^[A-Za-z0-9]{8,15}$", ErrorMessage = "Usa entre 8 y 15 letras o números, sin espacios ni guiones.")]
    [MaxLength(15)] public string IdentityDocument { get; set; } = string.Empty;
    [Required] public int LocationId { get; set; }
    [Required] public int PositionId { get; set; }
    [Required] public int SupervisorId { get; set; }
    [Required, DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(3);
    [Required, DataType(DataType.Date)] public DateTime AccessFrom { get; set; } = DateTime.Today;
    public List<SelectListItem> Locations { get; set; } = [];
    public List<SelectListItem> Supervisors { get; set; } = [];
}
