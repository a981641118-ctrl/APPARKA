using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace ApparkaTrainingFlowOnline.Services;

public record WorkflowResult(bool Success, string Message);
public record ValidationCodeGenerationResult(
    WorkflowResult Result,
    string? Code = null,
    DateTimeOffset? ExpiresAt = null);

public class TrainingWorkflowService(
    AppDbContext db,
    CurrentUserService current,
    TrainingScheduleService schedule,
    AuditService audit,
    ValidationCodeService validationCodes,
    IOptions<TrainingOptions> options)
{
    private readonly TrainingOptions _options = options.Value;

    public async Task<ValidationCodeGenerationResult> GenerateValidationCodeAsync(int evidenceId, int supervisorId)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment).ThenInclude(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == evidenceId);

        if (evidence is null)
            return new(new(false, "Actividad no encontrada."));

        if (evidence.Assignment.SupervisorId != supervisorId && current.Role != AppRoles.Administrator)
            return new(new(false, "No estás asignado a este colaborador."));

        // Actualiza el estado antes de generar el código para no depender de datos desfasados en pantalla.
        await schedule.RefreshAssignmentStatusAsync(evidence.Assignment);

        if (evidence.Status != EvidenceStatus.Available)
            return new(new(false, "Solo se puede generar un código para una actividad disponible."));

        if (nowUtc < evidence.AvailableFrom || nowUtc > evidence.DueAt)
            return new(new(false, "La actividad está fuera de su plazo programado."));

        var validityMinutes = Math.Max(1, _options.ValidationCodeMinutes);
        var executionStrategy = db.Database.CreateExecutionStrategy();
        var generation = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var code = validationCodes.Generate();
            var expiresAt = nowUtc.AddMinutes(validityMinutes);

            // Un código nuevo reemplaza a cualquier código activo anterior de la misma evidencia.
            var previousCodes = await db.ValidationSessions
                .Where(x => x.EvidenceId == evidenceId && x.UsedAt == null && x.ExpiresAt > nowUtc)
                .ToListAsync();

            foreach (var previous in previousCodes)
                previous.ExpiresAt = nowUtc;

            db.ValidationSessions.Add(new ValidationSession
            {
                EvidenceId = evidenceId,
                GeneratedById = supervisorId,
                CodeHash = validationCodes.Hash(code),
                ExpiresAt = expiresAt
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (Code: code, ExpiresAt: expiresAt, ReplacedCount: previousCodes.Count);
        });

        await audit.WriteAsync(
            "VALIDATION_CODE_CREATED",
            nameof(ActivityEvidence),
            evidenceId,
            $"Código temporal creado para la evidencia {evidence.Sequence}; vence en {validityMinutes} minutos. Se invalidaron {generation.ReplacedCount} código(s) anterior(es)." );

        return new(
            new(true, "Código generado correctamente."),
            generation.Code,
            generation.ExpiresAt);
    }

    public async Task<WorkflowResult> StartActivityAsync(int evidenceId, int collaboratorId, string? code)
    {
        if (!validationCodes.TryNormalize(code, out var normalizedCode))
        {
            await RegisterInvalidCodeAsync(evidenceId, "Formato inválido: el código debe contener seis dígitos.");
            return new(false, "Ingresa el código de seis dígitos generado por el supervisor.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment).ThenInclude(x => x.Activities)
            .Include(x => x.Template).ThenInclude(x => x.Questions)
            .Include(x => x.QuestionSelections)
            .FirstOrDefaultAsync(x => x.Id == evidenceId);

        if (evidence is null)
            return new(false, "Actividad no encontrada.");

        if (evidence.Assignment.CollaboratorId != collaboratorId)
            return new(false, "No tienes acceso a esta actividad.");

        await schedule.RefreshAssignmentStatusAsync(evidence.Assignment);

        if (evidence.Status == EvidenceStatus.InProgress)
            return new(false, "La actividad ya fue iniciada. Continúa con el cuestionario.");

        if (evidence.Status != EvidenceStatus.Available)
            return new(false, "La actividad no está disponible para iniciar.");

        if (nowUtc < evidence.AvailableFrom || nowUtc > evidence.DueAt)
            return new(false, "La actividad está fuera de su plazo programado.");

        if (evidence.Sequence > 1 && !evidence.Assignment.Activities.Any(x =>
                x.Sequence == evidence.Sequence - 1 && x.Status == EvidenceStatus.Completed))
            return new(false, "Primero debes completar la actividad anterior.");

        var questionBank = evidence.Template.Questions.Where(x => x.IsActive).ToList();
        if (questionBank.Count != 4)
            return new(false, "Esta actividad todavía no tiene configurado su banco de cuatro preguntas.");

        var recentInvalidAttempts = await db.AuditLogs.CountAsync(x =>
            x.Action == "INVALID_VALIDATION_CODE"
            && x.IpAddress == current.Ip
            && x.CreatedAt > nowUtc.AddMinutes(-5));

        if (recentInvalidAttempts >= 5)
            return new(false, "Se alcanzó el límite de intentos. Espera cinco minutos o solicita un nuevo código.");

        var codeHash = validationCodes.Hash(normalizedCode);
        var session = await db.ValidationSessions
            .AsNoTracking()
            .Where(x => x.EvidenceId == evidenceId && x.CodeHash == codeHash)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (session is null)
        {
            await RegisterInvalidCodeAsync(evidenceId, "El código no corresponde a esta actividad.");
            return new(false, "El código no corresponde a esta actividad. Solicita al supervisor que genere uno nuevo.");
        }

        if (session.UsedAt is not null)
        {
            await RegisterInvalidCodeAsync(evidenceId, "El código ya había sido utilizado.");
            return new(false, "Este código ya fue utilizado. Solicita uno nuevo al supervisor.");
        }

        if (session.ExpiresAt <= nowUtc)
        {
            await RegisterInvalidCodeAsync(evidenceId, "El código estaba vencido o fue reemplazado por uno más reciente.");
            return new(false, "El código venció o fue reemplazado. Solicita al supervisor un código nuevo.");
        }

        var executionStrategy = db.Database.CreateExecutionStrategy();
        var codeConsumed = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            // Consumo atómico: evita que dos solicitudes utilicen el mismo código al mismo tiempo.
            var consumedRows = await db.ValidationSessions
                .Where(x => x.Id == session.Id && x.UsedAt == null && x.ExpiresAt > nowUtc)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UsedAt, nowUtc)
                    .SetProperty(x => x.UsedById, collaboratorId)
                    .SetProperty(x => x.UsedIp, current.Ip));

            if (consumedRows != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var challengeText = await db.ActivityTemplates
                .Where(x => x.Id == evidence.TemplateId)
                .Select(x => x.PracticalChallenge)
                .FirstAsync();

            var challenges = challengeText.Split(
                "||",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            evidence.StartedAt = nowUtc;
            evidence.Status = EvidenceStatus.InProgress;
            evidence.StartIp = current.Ip;
            evidence.StartUserAgent = current.UserAgent;
            evidence.StartDeviceId = current.DeviceId;
            evidence.AssignedChallenge = challenges.Length == 0
                ? challengeText
                : challenges[RandomNumberGenerator.GetInt32(challenges.Length)];

            if (evidence.QuestionSelections.Count == 0)
            {
                var selectedQuestions = Shuffle(questionBank).Take(3).ToList();
                for (var index = 0; index < selectedQuestions.Count; index++)
                {
                    evidence.QuestionSelections.Add(new ActivityQuestionSelection
                    {
                        QuestionId = selectedQuestions[index].Id,
                        DisplayOrder = index + 1,
                        OptionOrder = RandomOptionOrder()
                    });
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        });

        if (!codeConsumed)
        {
            await RegisterInvalidCodeAsync(evidenceId, "El código fue consumido por otra solicitud o venció durante la validación.");
            return new(false, "El código ya no está disponible. Solicita uno nuevo al supervisor.");
        }

        await audit.WriteAsync(
            "ACTIVITY_STARTED",
            nameof(ActivityEvidence),
            evidenceId,
            "Actividad iniciada con un código temporal válido y consumido una sola vez.");

        return new(true, "Código válido. La actividad fue iniciada correctamente.");
    }

    public async Task<WorkflowResult> SubmitActivityAnswersAsync(
        int evidenceId,
        int collaboratorId,
        IDictionary<int, string>? answers)
    {
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment)
            .Include(x => x.QuestionSelections).ThenInclude(x => x.Question)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == evidenceId);

        if (evidence is null) return new(false, "Actividad no encontrada.");
        if (evidence.Assignment.CollaboratorId != collaboratorId) return new(false, "Acceso denegado.");
        if (evidence.Status != EvidenceStatus.InProgress) return new(false, "Esta actividad ya fue respondida o no se ha iniciado.");
        if (evidence.Answers.Count != 0) return new(false, "La evidencia solo puede responderse una vez.");

        var questions = evidence.QuestionSelections
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Question)
            .ToList();
        if (answers is null || questions.Count == 0 || questions.Any(q => !answers.ContainsKey(q.Id)))
            return new(false, "Responde todas las preguntas.");

        foreach (var question in questions)
        {
            var selected = answers[question.Id]?.Trim().ToUpperInvariant() ?? string.Empty;
            if (selected is not ("A" or "B" or "C" or "D"))
                return new(false, "Se recibió una alternativa inválida. Vuelve a seleccionar tus respuestas.");

            db.ActivityAnswers.Add(new ActivityAnswer
            {
                EvidenceId = evidence.Id,
                QuestionId = question.Id,
                SelectedOption = selected,
                IsCorrect = selected == question.CorrectOption
            });
        }

        var correct = questions.Count(q =>
            answers[q.Id].Trim().Equals(q.CorrectOption, StringComparison.OrdinalIgnoreCase));

        evidence.KnowledgeScore = (int)Math.Round(correct * 100d / questions.Count);
        evidence.CollaboratorSubmittedAt = DateTimeOffset.UtcNow;
        evidence.Status = EvidenceStatus.AwaitingSupervisor;

        await db.SaveChangesAsync();
        await audit.WriteAsync(
            "ACTIVITY_ANSWERS_SUBMITTED",
            nameof(ActivityEvidence),
            evidenceId,
            $"Cuestionario enviado con {evidence.KnowledgeScore}%.",
            evidence.KnowledgeScore < 70 ? AuditSeverity.Warning : AuditSeverity.Info);

        return new(true, "Respuestas registradas. El supervisor debe validar la práctica.");
    }

    public async Task<WorkflowResult> SubmitSupervisorReviewAsync(
        int evidenceId,
        int supervisorId,
        IReadOnlyList<(string Criterion, bool IsCritical, RatingValue Rating, string? Observation)> rubric,
        string feedback)
    {
        var evidence = await db.ActivityEvidences
            .Include(x => x.Assignment).ThenInclude(x => x.Activities)
            .Include(x => x.Rubric)
            .FirstOrDefaultAsync(x => x.Id == evidenceId);

        if (evidence is null) return new(false, "Actividad no encontrada.");
        if (evidence.Assignment.SupervisorId != supervisorId && current.Role != AppRoles.Administrator)
            return new(false, "No eres el supervisor asignado.");
        if (evidence.Status != EvidenceStatus.AwaitingSupervisor)
            return new(false, "La actividad no está pendiente de validación.");
        if (rubric.Count != 5 || rubric.Any(x => x.Rating == RatingValue.NotEvaluated))
            return new(false, "Debes evaluar los cinco criterios.");
        if (rubric.Any(x =>
                (x.Rating is RatingValue.PartiallyComplies or RatingValue.DoesNotComply)
                && string.IsNullOrWhiteSpace(x.Observation)))
            return new(false, "Agrega una observación en los criterios que no cumplen totalmente.");

        foreach (var item in rubric)
        {
            db.RubricEvaluations.Add(new RubricEvaluation
            {
                EvidenceId = evidence.Id,
                Criterion = item.Criterion,
                IsCritical = item.IsCritical,
                Rating = item.Rating,
                Observation = item.Observation?.Trim()
            });
        }

        static int RatingScore(RatingValue value) => value switch
        {
            RatingValue.Complies => 100,
            RatingValue.PartiallyComplies => 60,
            RatingValue.DoesNotComply => 0,
            _ => 0
        };

        var nowUtc = DateTimeOffset.UtcNow;
        evidence.PracticalScore = (int)Math.Round(rubric.Average(x => RatingScore(x.Rating)));
        evidence.FinalScore = (int)Math.Round(evidence.KnowledgeScore * 0.4 + evidence.PracticalScore * 0.6);
        evidence.SupervisorFeedback = feedback?.Trim();
        evidence.SupervisorSubmittedAt = nowUtc;
        evidence.CompletedAt = nowUtc;
        evidence.SupervisorIp = current.Ip;
        evidence.SupervisorUserAgent = current.UserAgent;
        evidence.SupervisorDeviceId = current.DeviceId;
        evidence.PossibleSharedDevice = !string.IsNullOrWhiteSpace(evidence.StartDeviceId)
            && string.Equals(evidence.StartDeviceId, current.DeviceId, StringComparison.Ordinal);
        evidence.Status = EvidenceStatus.Completed;

        var next = evidence.Assignment.Activities.FirstOrDefault(x => x.Sequence == evidence.Sequence + 1);
        if (next is not null && nowUtc >= next.AvailableFrom)
            next.Status = EvidenceStatus.Available;

        await db.SaveChangesAsync();
        await schedule.RefreshAssignmentStatusAsync(evidence.Assignment);
        await audit.WriteAsync(
            "ACTIVITY_COMPLETED",
            nameof(ActivityEvidence),
            evidenceId,
            $"Evidencia cerrada. Conocimiento: {evidence.KnowledgeScore}%, práctica: {evidence.PracticalScore}%, final: {evidence.FinalScore}%."
            + (evidence.PossibleSharedDevice ? " Coincidencia de dispositivo detectada." : string.Empty),
            evidence.PossibleSharedDevice ? AuditSeverity.Warning : AuditSeverity.Info);

        return new(true, "Evidencia registrada. No se habilitan reintentos para esta actividad.");
    }

    public async Task<(WorkflowResult Result, FinalExamAttempt? Attempt)> StartFinalExamAsync(
        int assignmentId,
        int collaboratorId)
    {
        var assignment = await db.TrainingAssignments
            .Include(x => x.Activities)
            .Include(x => x.FinalExamAttempts)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment is null || assignment.CollaboratorId != collaboratorId)
            return (new(false, "Asignación no encontrada."), null);

        await schedule.RefreshAssignmentStatusAsync(assignment);

        if (assignment.Status != TrainingStatus.ReadyForFinalExam)
            return (new(false, "Completa las seis evidencias antes del examen final."), null);

        var activeAttempt = assignment.FinalExamAttempts.FirstOrDefault(x => x.CompletedAt == null);
        if (activeAttempt is not null)
            return (new(true, "Se retomará el intento en curso."), activeAttempt);

        if (assignment.FinalExamAttempts.Count >= _options.FinalExamMaxAttempts)
            return (new(false, $"Ya utilizaste los {_options.FinalExamMaxAttempts} intentos permitidos."), null);

        var questionBank = await db.Questions
            .Where(x => x.IsFinalExamQuestion && x.IsActive)
            .ToListAsync();
        if (questionBank.Count < 10)
            return (new(false, "No existen suficientes preguntas configuradas para iniciar el examen final."), null);

        var questions = questionBank
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .Take(10)
            .ToList();

        var attempt = new FinalExamAttempt
        {
            AssignmentId = assignmentId,
            AttemptNumber = assignment.FinalExamAttempts.Count + 1,
            Answers = questions.Select((q, index) => new FinalExamAnswer
            {
                QuestionId = q.Id,
                DisplayOrder = index + 1,
                OptionOrder = RandomOptionOrder()
            }).ToList()
        };

        db.FinalExamAttempts.Add(attempt);
        await db.SaveChangesAsync();
        await audit.WriteAsync(
            "FINAL_EXAM_STARTED",
            nameof(TrainingAssignment),
            assignmentId,
            $"Intento final {attempt.AttemptNumber} iniciado.");

        return (new(true, "Examen iniciado."), attempt);
    }

    public async Task<WorkflowResult> SubmitFinalExamAsync(
        int attemptId,
        int collaboratorId,
        IDictionary<int, string>? answers)
    {
        var attempt = await db.FinalExamAttempts
            .Include(x => x.Assignment)
            .Include(x => x.Answers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == attemptId);

        if (attempt is null || attempt.Assignment.CollaboratorId != collaboratorId)
            return new(false, "Intento no encontrado.");
        if (attempt.CompletedAt is not null)
            return new(false, "Este intento ya fue enviado.");
        if (answers is null || attempt.Answers.Any(x => !answers.ContainsKey(x.QuestionId)))
            return new(false, "Responde todas las preguntas.");

        foreach (var answer in attempt.Answers)
        {
            var selected = answers[answer.QuestionId]?.Trim().ToUpperInvariant() ?? string.Empty;
            if (selected is not ("A" or "B" or "C" or "D"))
                return new(false, "Se recibió una alternativa inválida.");

            answer.SelectedOption = selected;
            answer.IsCorrect = selected == answer.Question.CorrectOption;
        }

        attempt.Score = (int)Math.Round(attempt.Answers.Count(x => x.IsCorrect) * 100d / attempt.Answers.Count);
        attempt.Passed = attempt.Score >= _options.FinalExamPassingScore;
        attempt.CompletedAt = DateTimeOffset.UtcNow;

        if (attempt.Passed)
            attempt.Assignment.Status = TrainingStatus.Apt;
        else if (attempt.AttemptNumber >= _options.FinalExamMaxAttempts)
            attempt.Assignment.Status = TrainingStatus.NotApt;

        await db.SaveChangesAsync();
        await audit.WriteAsync(
            "FINAL_EXAM_SUBMITTED",
            nameof(FinalExamAttempt),
            attempt.Id,
            $"Intento {attempt.AttemptNumber}: {attempt.Score}%. Resultado: {(attempt.Passed ? "Apto" : "No aprobado")}.",
            attempt.Passed ? AuditSeverity.Info : AuditSeverity.Warning);

        return new(true, attempt.Passed
            ? "Examen aprobado. El colaborador ha sido calificado como APTO."
            : attempt.AttemptNumber >= _options.FinalExamMaxAttempts
                ? $"No alcanzó el puntaje mínimo en {_options.FinalExamMaxAttempts} intentos. Resultado: NO APTO."
                : $"No alcanzaste {_options.FinalExamPassingScore}%. Te quedan {_options.FinalExamMaxAttempts - attempt.AttemptNumber} intento(s).");
    }

    private async Task RegisterInvalidCodeAsync(int evidenceId, string detail)
    {
        await audit.WriteAsync(
            "INVALID_VALIDATION_CODE",
            nameof(ActivityEvidence),
            evidenceId,
            detail,
            AuditSeverity.Warning);
    }

    private static List<T> Shuffle<T>(IReadOnlyList<T> source)
    {
        var result = source.ToList();
        for (var index = result.Count - 1; index > 0; index--)
        {
            var swapWith = RandomNumberGenerator.GetInt32(index + 1);
            (result[index], result[swapWith]) = (result[swapWith], result[index]);
        }
        return result;
    }

    private static string RandomOptionOrder() => string.Concat(Shuffle(new[] { 'A', 'B', 'C', 'D' }));
}
