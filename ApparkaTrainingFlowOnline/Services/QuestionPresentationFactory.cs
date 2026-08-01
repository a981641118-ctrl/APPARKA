using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.ViewModels;

namespace ApparkaTrainingFlowOnline.Services;

public static class QuestionPresentationFactory
{
    public static QuestionPresentationViewModel Create(Question question, string? optionOrder)
    {
        var validOrder = string.IsNullOrWhiteSpace(optionOrder)
            ? "ABCD"
            : new string(optionOrder.Where(x => x is 'A' or 'B' or 'C' or 'D').Distinct().ToArray());

        if (validOrder.Length != 4)
            validOrder = "ABCD";

        return new QuestionPresentationViewModel
        {
            QuestionId = question.Id,
            Prompt = question.Prompt,
            Options = validOrder.Select(key => new QuestionOptionViewModel
            {
                Key = key.ToString(),
                Text = GetOptionText(question, key)
            }).ToList()
        };
    }

    public static string GetOptionText(Question question, char key) => key switch
    {
        'A' => question.OptionA,
        'B' => question.OptionB,
        'C' => question.OptionC,
        'D' => question.OptionD,
        _ => string.Empty
    };
}
