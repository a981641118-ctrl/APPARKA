namespace ApparkaTrainingFlowOnline.Models;

public sealed class LearningModuleContent
{
    public string Introduction { get; set; } = string.Empty;
    public List<LearningContentSection> Sections { get; set; } = [];
    public string? ConfirmationNote { get; set; }
}

public sealed class LearningContentSection
{
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public List<string> Items { get; set; } = [];
    public bool IsInferred { get; set; }
}
