namespace GameHub.Data.Seed;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    public string DemoUsername { get; set; } = string.Empty;
    public string DemoPassword { get; set; } = string.Empty;
}