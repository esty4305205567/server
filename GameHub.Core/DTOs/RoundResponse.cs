namespace GameHub.Core.DTOs;

public class RoundResponse
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public IReadOnlyList<int> Options { get; set; } = Array.Empty<int>();
}