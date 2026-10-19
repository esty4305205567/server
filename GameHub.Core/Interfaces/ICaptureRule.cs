using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface ICaptureRule
{
    GameType Type { get; }

    CaptureRuleResult Evaluate(CaptureContext context);
}