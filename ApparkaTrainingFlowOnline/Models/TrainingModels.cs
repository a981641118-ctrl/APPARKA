using System.ComponentModel.DataAnnotations;

namespace ApparkaTrainingFlowOnline.Models;

public class TrainingAssignment
{
    public int Id { get; set; }
    public int CollaboratorId { get; set; }
    public AppUser Collaborator { get; set; } = null!;
    public int SupervisorId { get; set; }
    public AppUser Supervisor { get; set; } = null!;
    public int CreatedById { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public DateOnly AccessFrom { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TrainingStatus Status { get; set; } = TrainingStatus.Preboarding;
    public int? OverallActivityScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ActivityEvidence> Activities { get; set; } = [];
    public ICollection<FinalExamAttempt> FinalExamAttempts { get; set; } = [];
}

public class ActivityTemplate
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public int WeekNumber { get; set; }
    [Required, MaxLength(140)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(1200)] public string Instructions { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string PracticalChallenge { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Question> Questions { get; set; } = [];
}

public class ActivityEvidence
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public TrainingAssignment Assignment { get; set; } = null!;
    public int TemplateId { get; set; }
    public ActivityTemplate Template { get; set; } = null!;
    public int Sequence { get; set; }
    public int WeekNumber { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public EvidenceStatus Status { get; set; } = EvidenceStatus.Scheduled;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CollaboratorSubmittedAt { get; set; }
    public DateTimeOffset? SupervisorSubmittedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    [MaxLength(500)] public string? AssignedChallenge { get; set; }
    public int KnowledgeScore { get; set; }
    public int PracticalScore { get; set; }
    public int FinalScore { get; set; }
    [MaxLength(80)] public string? StartIp { get; set; }
    [MaxLength(600)] public string? StartUserAgent { get; set; }
    [MaxLength(80)] public string? StartDeviceId { get; set; }
    [MaxLength(80)] public string? SupervisorIp { get; set; }
    [MaxLength(600)] public string? SupervisorUserAgent { get; set; }
    [MaxLength(80)] public string? SupervisorDeviceId { get; set; }
    public bool PossibleSharedDevice { get; set; }
    [MaxLength(1000)] public string? SupervisorFeedback { get; set; }
    public ICollection<ActivityAnswer> Answers { get; set; } = [];
    public ICollection<RubricEvaluation> Rubric { get; set; } = [];
    public ICollection<ValidationSession> ValidationSessions { get; set; } = [];
}

public class Question
{
    public int Id { get; set; }
    public int? ActivityTemplateId { get; set; }
    public ActivityTemplate? ActivityTemplate { get; set; }
    public bool IsFinalExamQuestion { get; set; }
    [Required, MaxLength(500)] public string Prompt { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string OptionA { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string OptionB { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string OptionC { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string OptionD { get; set; } = string.Empty;
    [Required, MaxLength(1)] public string CorrectOption { get; set; } = "A";
    [Required, MaxLength(1000)] public string Explanation { get; set; } = string.Empty;
}

public class ActivityAnswer
{
    public int Id { get; set; }
    public int EvidenceId { get; set; }
    public ActivityEvidence Evidence { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    [MaxLength(1)] public string SelectedOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class RubricEvaluation
{
    public int Id { get; set; }
    public int EvidenceId { get; set; }
    public ActivityEvidence Evidence { get; set; } = null!;
    [Required, MaxLength(220)] public string Criterion { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public RatingValue Rating { get; set; }
    [MaxLength(500)] public string? Observation { get; set; }
}

public class ValidationSession
{
    public int Id { get; set; }
    public int EvidenceId { get; set; }
    public ActivityEvidence Evidence { get; set; } = null!;
    public int GeneratedById { get; set; }
    public AppUser GeneratedBy { get; set; } = null!;
    [Required, MaxLength(64)] public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public int? UsedById { get; set; }
    [MaxLength(80)] public string? UsedIp { get; set; }
}

public class FinalExamAttempt
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public TrainingAssignment Assignment { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int Score { get; set; }
    public bool Passed { get; set; }
    public ICollection<FinalExamAnswer> Answers { get; set; } = [];
}

public class FinalExamAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public FinalExamAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    [MaxLength(1)] public string SelectedOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class LearningMaterial
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    [Required, MaxLength(160)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(1500)] public string Summary { get; set; } = string.Empty;
    [MaxLength(500)] public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
}

public class LearningMaterialProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int MaterialId { get; set; }
    public LearningMaterial Material { get; set; } = null!;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    [MaxLength(120)] public string Actor { get; set; } = string.Empty;
    [MaxLength(60)] public string Role { get; set; } = string.Empty;
    [MaxLength(100)] public string Action { get; set; } = string.Empty;
    [MaxLength(80)] public string EntityType { get; set; } = string.Empty;
    [MaxLength(80)] public string EntityId { get; set; } = string.Empty;
    [MaxLength(1200)] public string Detail { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    [MaxLength(80)] public string? IpAddress { get; set; }
    [MaxLength(600)] public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
