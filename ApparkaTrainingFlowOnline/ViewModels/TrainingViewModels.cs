using ApparkaTrainingFlowOnline.Models;

namespace ApparkaTrainingFlowOnline.ViewModels;

public class ActivityPageViewModel
{
    public ActivityEvidence Evidence { get; set; } = null!;
    public string? OneTimeCode { get; set; }
    public Dictionary<int, string> Answers { get; set; } = [];
    public List<QuestionPresentationViewModel> Questions { get; set; } = [];
}

public class SupervisorReviewViewModel
{
    public int EvidenceId { get; set; }
    public int? DashboardSupervisorId { get; set; }
    public ActivityEvidence Evidence { get; set; } = null!;
    public List<RubricInput> Items { get; set; } = [];
    public OverallAssessmentValue OverallAssessment { get; set; }
    public ObservedStrengthValue ObservedStrength { get; set; }
    public string? OverallEvidence { get; set; }
    public string? MainImprovement { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public bool GuideCompleted { get; set; }
}

public class RubricInput
{
    public string Criterion { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public RatingValue Rating { get; set; }
    public string? Observation { get; set; }
    public string? GuidanceProvided { get; set; }
}

public class FinalExamViewModel
{
    public FinalExamAttempt Attempt { get; set; } = null!;
    public Dictionary<int, string> Answers { get; set; } = [];
    public List<QuestionPresentationViewModel> Questions { get; set; } = [];
}

public class SupervisorDashboardViewModel
{
    public PagedResult<TrainingAssignment> Assignments { get; set; } = new();
    public PagedResult<SupervisorActionRowViewModel> PendingActions { get; set; } = new();
    public PagedResult<SupervisorActionRowViewModel> StartableActions { get; set; } = new();
    public IReadOnlyList<AppUser> Supervisors { get; set; } = [];
    public IReadOnlyList<Location> Locations { get; set; } = [];
    public int? SelectedSupervisorId { get; set; }
    public bool IsAdministrator { get; set; }
    public bool GuideCompleted { get; set; }
    public int ActiveAssignmentCount { get; set; }
    public string Search { get; set; } = string.Empty;
    public TrainingStatus? Status { get; set; }
    public int? LocationId { get; set; }
    public string? SelectedSupervisorName => Supervisors
        .FirstOrDefault(x => x.Id == SelectedSupervisorId)?.FullName;
}

public class SupervisorActionRowViewModel
{
    public TrainingAssignment Assignment { get; set; } = null!;
    public ActivityEvidence Evidence { get; set; } = null!;
}

public class QuestionPresentationViewModel
{
    public int QuestionId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public List<QuestionOptionViewModel> Options { get; set; } = [];
}

public class QuestionOptionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class LearningMaterialPageViewModel
{
    public LearningMaterial Material { get; set; } = null!;
    public LearningModuleContent Content { get; set; } = new();
    public bool IsCompleted { get; set; }
}
