using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class LootCaptureRule : ICaptureRule
{
    public GameType Type => GameType.LootCollection;

    public CaptureRuleResult Evaluate(CaptureContext context)
    {
        var elapsedMs = (context.NowUtc - context.Session.StartedAtUtc!.Value).TotalMilliseconds;

        if (elapsedMs < context.Item.AppearAtMs!.Value)
        {
            return CaptureRuleResult.Conflict("התיבה עדיין לא הופיעה");
        }

        if (elapsedMs >= context.Item.DisappearAtMs!.Value)
        {
            return CaptureRuleResult.Conflict("התיבה כבר נעלמה");
        }

        return CaptureRuleResult.Allowed();
    }
}