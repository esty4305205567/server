using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class QuizCaptureRule : ICaptureRule
{
    public GameType Type => GameType.QuickQuiz;

    public CaptureRuleResult Evaluate(CaptureContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Answer))
        {
            return CaptureRuleResult.BadRequest("יש להקליד תשובה");
        }

        if (context.Item.Order != context.CurrentRoundOrder)
        {
            return CaptureRuleResult.BadRequest("השאלה הזאת עדיין לא התחילה");
        }

        if (context.Answer.Trim() != context.Item.CorrectAnswer)
        {
            return CaptureRuleResult.BadRequest("לא נכון, נסו שוב");
        }

        return CaptureRuleResult.Allowed();
    }
}