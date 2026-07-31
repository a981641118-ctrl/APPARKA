namespace ApparkaTrainingFlowOnline.Models;

public static class UiExtensions
{
    public static string Label(this TrainingStatus value) => value switch
    {
        TrainingStatus.Preboarding => "Preparación previa",
        TrainingStatus.InTraining => "En entrenamiento",
        TrainingStatus.ReadyForFinalExam => "Listo para examen",
        TrainingStatus.Apt => "Apto",
        TrainingStatus.NotApt => "No apto",
        TrainingStatus.Cancelled => "Cancelado",
        _ => value.ToString()
    };

    public static string Label(this EvidenceStatus value) => value switch
    {
        EvidenceStatus.Scheduled => "Programada",
        EvidenceStatus.Available => "Disponible",
        EvidenceStatus.InProgress => "En proceso",
        EvidenceStatus.AwaitingSupervisor => "Espera validación",
        EvidenceStatus.Completed => "Completada",
        EvidenceStatus.Expired => "Vencida",
        _ => value.ToString()
    };

    public static string Label(this RatingValue value) => value switch
    {
        RatingValue.NotEvaluated => "Sin evaluar",
        RatingValue.Complies => "Cumple",
        RatingValue.PartiallyComplies => "Cumple parcialmente",
        RatingValue.DoesNotComply => "No cumple",
        _ => value.ToString()
    };

    public static string RoleLabel(this string role) => role switch
    {
        AppRoles.Administrator => "Administrador",
        AppRoles.Supervisor => "Supervisor",
        AppRoles.Hr => "Recursos Humanos",
        AppRoles.Collaborator => "Nuevo colaborador",
        _ => role
    };
}
