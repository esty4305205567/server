using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class NumberRaceCaptureRule : ICaptureRule
{
    public GameType Type => GameType.NumberRace;

    public CaptureRuleResult Evaluate(CaptureContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Answer))
        {
            return CaptureRuleResult.BadRequest("יש לבחור מספר");
        }

        if (context.Item.Order != context.CurrentRoundOrder)
        {
            return CaptureRuleResult.BadRequest("הסבב הזה עדיין לא התחיל");
        }

        var answer = context.Answer.Trim();

        var options = (context.Item.Options ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!options.Contains(answer))
        {
            return CaptureRuleResult.BadRequest("המספר אינו חלק מהסבב");
        }

        if (answer != context.Item.CorrectAnswer)
        {
            return CaptureRuleResult.BadRequest("לא נכון, נסו שוב");
        }

        return CaptureRuleResult.Allowed();
    }
}