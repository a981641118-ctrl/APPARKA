using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Services;

public class TrainingScheduleService(AppDbContext db, PeruClock clock)
{
    private static readonly (int StartDay, int EndDay)[] Windows =
    [
        (0, 2), (3, 6),
        (7, 9), (10, 13),
        (14, 16), (17, 20)
    ];

    public async Task GenerateActivitiesAsync(TrainingAssignment assignment)
    {
        var templates = await db.ActivityTemplates.Where(x => x.IsActive).OrderBy(x => x.Sequence).ToListAsync();
        if (templates.Count != 6)
            throw new InvalidOperationException("Deben existir exactamente seis plantillas activas.");

        for (var index = 0; index < templates.Count; index++)
        {
            var template = templates[index];
            var window = Windows[index];
            var from = clock.At(assignment.StartDate.AddDays(window.StartDay), TimeOnly.MinValue);
            var due = clock.At(assignment.StartDate.AddDays(window.EndDay), new TimeOnly(23, 59, 59));
            assignment.Activities.Add(new ActivityEvidence
            {
                TemplateId = template.Id,
                Sequence = template.Sequence,
                WeekNumber = template.WeekNumber,
                AvailableFrom = from,
                DueAt = due,
                Status = index == 0 && clock.UtcNow >= from ? EvidenceStatus.Available : EvidenceStatus.Scheduled
            });
        }
    }

    public void ReprogramActivities(TrainingAssignment assignment, DateOnly startDate)
    {
        assignment.StartDate = startDate;
        assignment.EndDate = startDate.AddDays(20);

        var now = clock.UtcNow;
        var activities = assignment.Activities.OrderBy(x => x.Sequence).ToList();
        foreach (var activity in activities)
        {
            if (activity.Sequence < 1 || activity.Sequence > Windows.Length)
                continue;

            var window = Windows[activity.Sequence - 1];
            activity.AvailableFrom = clock.At(startDate.AddDays(window.StartDay), TimeOnly.MinValue);
            activity.DueAt = clock.At(startDate.AddDays(window.EndDay), new TimeOnly(23, 59, 59));

            if (activity.Status is EvidenceStatus.Completed or EvidenceStatus.InProgress or EvidenceStatus.AwaitingSupervisor)
                continue;

            var previousCompleted = activity.WasExceptionallyUnlocked || activity.Sequence == 1 || activities
                .Any(x => x.Sequence == activity.Sequence - 1 && x.Status == EvidenceStatus.Completed);
            activity.Status = now < activity.AvailableFrom
                ? EvidenceStatus.Scheduled
                : now > activity.DueAt
                    ? EvidenceStatus.Expired
                    : previousCompleted
                        ? EvidenceStatus.Available
                        : EvidenceStatus.Scheduled;
        }

        if (assignment.Status is not (TrainingStatus.Apt or TrainingStatus.AptObserved or TrainingStatus.NotApt or TrainingStatus.Cancelled))
        {
            assignment.Status = clock.Today < assignment.StartDate
                ? TrainingStatus.Preboarding
                : activities.All(x => x.Status == EvidenceStatus.Completed)
                    ? TrainingStatus.ReadyForFinalExam
                    : TrainingStatus.InTraining;
        }
    }

    public async Task RefreshAssignmentStatusAsync(TrainingAssignment assignment)
    {
        var now = clock.UtcNow;
        if (assignment.Status is TrainingStatus.Apt or TrainingStatus.AptObserved or TrainingStatus.NotApt or TrainingStatus.Cancelled)
            return;

        if (clock.Today < assignment.StartDate)
            assignment.Status = TrainingStatus.Preboarding;
        else if (assignment.Activities.All(x => x.Status == EvidenceStatus.Completed))
            assignment.Status = TrainingStatus.ReadyForFinalExam;
        else
            assignment.Status = TrainingStatus.InTraining;

        foreach (var activity in assignment.Activities.OrderBy(x => x.Sequence))
        {
            // Corrige estados antiguos o desfasados: una actividad futura nunca debe mostrarse como disponible.
            if (activity.Status == EvidenceStatus.Available && now < activity.AvailableFrom)
                activity.Status = EvidenceStatus.Scheduled;

            if (activity.Status == EvidenceStatus.Scheduled && now >= activity.AvailableFrom)
            {
                var previousCompleted = activity.WasExceptionallyUnlocked || activity.Sequence == 1 || assignment.Activities
                    .Any(x => x.Sequence == activity.Sequence - 1 && x.Status == EvidenceStatus.Completed);
                if (previousCompleted) activity.Status = EvidenceStatus.Available;
            }
            if ((activity.Status is EvidenceStatus.Scheduled or EvidenceStatus.Available) && now > activity.DueAt)
            {
                activity.Status = EvidenceStatus.Expired;
                db.AuditLogs.Add(new AuditLog
                {
                    Actor = "Sistema",
                    Action = "ACTIVITY_EXPIRED",
                    EntityType = nameof(ActivityEvidence),
                    EntityId = activity.Id.ToString(),
                    Detail = $"La actividad {activity.Sequence} venció sin completarse; las actividades posteriores permanecen bloqueadas.",
                    Severity = AuditSeverity.Warning
                });
            }
        }

        assignment.OverallActivityScore = assignment.Activities.Any(x => x.Status == EvidenceStatus.Completed)
            ? (int)Math.Round(assignment.Activities.Where(x => x.Status == EvidenceStatus.Completed).Average(x => x.FinalScore))
            : null;
        await db.SaveChangesAsync();
    }

    public async Task RefreshOperationalStatusesAsync(int? supervisorId = null)
    {
        var now = clock.UtcNow;
        var today = clock.Today;
        var assignments = db.TrainingAssignments.Where(x =>
            x.Status != TrainingStatus.Apt
            && x.Status != TrainingStatus.AptObserved
            && x.Status != TrainingStatus.NotApt
            && x.Status != TrainingStatus.Cancelled);
        if (supervisorId is not null)
            assignments = assignments.Where(x => x.SupervisorId == supervisorId.Value);

        var evidences = db.ActivityEvidences.Where(x =>
            x.Assignment.Status != TrainingStatus.Apt
            && x.Assignment.Status != TrainingStatus.AptObserved
            && x.Assignment.Status != TrainingStatus.NotApt
            && x.Assignment.Status != TrainingStatus.Cancelled);
        if (supervisorId is not null)
            evidences = evidences.Where(x => x.Assignment.SupervisorId == supervisorId.Value);

        var expiringActivities = await evidences
            .Where(x => (x.Status == EvidenceStatus.Scheduled || x.Status == EvidenceStatus.Available)
                && now > x.DueAt)
            .Select(x => new { x.Id, x.Sequence })
            .ToListAsync();
        if (expiringActivities.Count > 0)
        {
            db.AuditLogs.AddRange(expiringActivities.Select(activity => new AuditLog
            {
                Actor = "Sistema",
                Action = "ACTIVITY_EXPIRED",
                EntityType = nameof(ActivityEvidence),
                EntityId = activity.Id.ToString(),
                Detail = $"La actividad {activity.Sequence} venció sin completarse; las actividades posteriores permanecen bloqueadas.",
                Severity = AuditSeverity.Warning
            }));
            await db.SaveChangesAsync();
            var expiringIds = expiringActivities.Select(x => x.Id).ToList();
            await db.ActivityEvidences
                .Where(x => expiringIds.Contains(x.Id)
                    && (x.Status == EvidenceStatus.Scheduled || x.Status == EvidenceStatus.Available))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, EvidenceStatus.Expired));
        }

        await evidences
            .Where(x => x.Status == EvidenceStatus.Available && now < x.AvailableFrom)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, EvidenceStatus.Scheduled));

        await evidences
            .Where(x => x.Status == EvidenceStatus.Scheduled
                && now >= x.AvailableFrom
                && now <= x.DueAt
                && (x.WasExceptionallyUnlocked || x.Sequence == 1 || db.ActivityEvidences.Any(previous =>
                    previous.AssignmentId == x.AssignmentId
                    && previous.Sequence == x.Sequence - 1
                    && previous.Status == EvidenceStatus.Completed)))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, EvidenceStatus.Available));

        await assignments
            .Where(x => x.StartDate > today && x.Status != TrainingStatus.Preboarding)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, TrainingStatus.Preboarding));

        await assignments
            .Where(x => x.StartDate <= today
                && x.Activities.Any(activity => activity.Status != EvidenceStatus.Completed)
                && x.Status != TrainingStatus.InTraining)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, TrainingStatus.InTraining));

        await assignments
            .Where(x => x.Activities.Any()
                && x.Activities.All(activity => activity.Status == EvidenceStatus.Completed)
                && x.Status != TrainingStatus.ReadyForFinalExam)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, TrainingStatus.ReadyForFinalExam));
    }
}
