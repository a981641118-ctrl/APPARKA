using ApparkaTrainingFlowOnline.Models;

namespace ApparkaTrainingFlowOnline.ViewModels;

public class ActivityPageViewModel
{
    public ActivityEvidence Evidence { get; set; } = null!;
    public string? OneTimeCode { get; set; }
    public Dictionary<int, string> Answers { get; set; } = [];
}

public class SupervisorReviewViewModel
{
    public int EvidenceId { get; set; }
    public ActivityEvidence Evidence { get; set; } = null!;
    public List<RubricInput> Items { get; set; } = [];
    public string Feedback { get; set; } = string.Empty;
}

public class RubricInput
{
    public string Criterion { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public RatingValue Rating { get; set; }
    public string? Observation { get; set; }
}

public class FinalExamViewModel
{
    public FinalExamAttempt Attempt { get; set; } = null!;
    public Dictionary<int, string> Answers { get; set; } = [];
}
