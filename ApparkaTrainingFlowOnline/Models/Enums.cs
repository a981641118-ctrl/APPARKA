using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.Models;

public enum TrainingStatus { Preboarding, InTraining, ReadyForFinalExam, Apt, NotApt, Cancelled }
public enum EvidenceStatus { Scheduled, Available, InProgress, AwaitingSupervisor, Completed, Expired }
public enum RatingValue
{
    [Display(Name = "Seleccionar")] NotEvaluated,
    [Display(Name = "Cumple")] Complies,
    [Display(Name = "Cumple parcialmente")] PartiallyComplies,
    [Display(Name = "No cumple")] DoesNotComply
}

public enum OverallAssessmentValue
{
    [Display(Name = "Seleccionar")] NotEvaluated,
    [Display(Name = "Desempeño destacado")] Outstanding,
    [Display(Name = "Cumplimiento esperado")] Expected,
    [Display(Name = "Requiere mejora")] NeedsImprovement
}

public enum ObservedStrengthValue
{
    [Display(Name = "Seleccionar")] NotSelected,
    [Display(Name = "Aplicación del procedimiento")] Procedure,
    [Display(Name = "Seguridad y prevención")] Safety,
    [Display(Name = "Uso del sistema o equipo")] SystemOrEquipment,
    [Display(Name = "Atención al cliente")] CustomerService,
    [Display(Name = "Verificación y registro")] VerificationAndRecord,
    [Display(Name = "No se identificó una fortaleza diferenciada")] NoDistinctStrength
}
public enum AuditSeverity { Info, Warning, Critical }
