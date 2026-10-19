namespace GameHub.Core.DTOs;

public class LootBoxResponse
{
    public int Id { get; set; }
    public int Points { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int AppearAtMs { get; set; }
    public int DisappearAtMs { get; set; }
}