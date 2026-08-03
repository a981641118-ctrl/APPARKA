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

    public async Task RefreshAssignmentStatusAsync(TrainingAssignment assignment)
    {
        var now = clock.UtcNow;
        if (assignment.Status is TrainingStatus.Apt or TrainingStatus.NotApt or TrainingStatus.Cancelled)
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
                var previousCompleted = activity.Sequence == 1 || assignment.Activities
                    .Any(x => x.Sequence == activity.Sequence - 1 && x.Status == EvidenceStatus.Completed);
                if (previousCompleted) activity.Status = EvidenceStatus.Available;
            }
            if ((activity.Status is EvidenceStatus.Scheduled or EvidenceStatus.Available) && now > activity.DueAt)
                activity.Status = EvidenceStatus.Expired;
        }

        assignment.OverallActivityScore = assignment.Activities.Any(x => x.Status == EvidenceStatus.Completed)
            ? (int)Math.Round(assignment.Activities.Where(x => x.Status == EvidenceStatus.Completed).Average(x => x.FinalScore))
            : null;
        await db.SaveChangesAsync();
    }
}
