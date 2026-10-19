using System.ComponentModel.DataAnnotations;

namespace GameHub.Core.DTOs;

public class CaptureRequest
{
    public const int MaxAnswerLength = 50;
    public const string InvalidItemMessage = "יש לבחור פריט תקין";
    public const string AnswerTooLongMessage = "התשובה ארוכה מדי, עד 50 תווים";

    [Range(1, int.MaxValue, ErrorMessage = InvalidItemMessage)]
    public int ItemId { get; set; }

    [StringLength(MaxAnswerLength, ErrorMessage = AnswerTooLongMessage)]
    public string? Answer { get; set; }
}