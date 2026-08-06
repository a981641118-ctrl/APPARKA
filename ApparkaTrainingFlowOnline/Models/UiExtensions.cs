namespace ApparkaTrainingFlowOnline.Models;

public static class UiExtensions
{
    public static string Label(this TrainingStatus value) => value switch
    {
        TrainingStatus.Preboarding => "Preparación previa",
        TrainingStatus.InTraining => "En entrenamiento",
        TrainingStatus.ReadyForFinalExam => "Listo para examen",
        TrainingStatus.Apt => "Apto",
        TrainingStatus.AptObserved => "Apto - observado",
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

    public static string Label(this OverallAssessmentValue value) => value switch
    {
        OverallAssessmentValue.Outstanding => "Desempeño destacado",
        OverallAssessmentValue.Expected => "Cumplimiento esperado",
        OverallAssessmentValue.NeedsImprovement => "Requiere mejora",
        _ => "Sin evaluación"
    };

    public static string Label(this ObservedStrengthValue value) => value switch
    {
        ObservedStrengthValue.Procedure => "Aplicación del procedimiento",
        ObservedStrengthValue.Safety => "Seguridad y prevención",
        ObservedStrengthValue.SystemOrEquipment => "Uso del sistema o equipo",
        ObservedStrengthValue.CustomerService => "Atención al cliente",
        ObservedStrengthValue.VerificationAndRecord => "Verificación y registro",
        ObservedStrengthValue.NoDistinctStrength => "No se identificó una fortaleza diferenciada",
        _ => "Sin selección"
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
