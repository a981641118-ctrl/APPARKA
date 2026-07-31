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
public enum AuditSeverity { Info, Warning, Critical }
