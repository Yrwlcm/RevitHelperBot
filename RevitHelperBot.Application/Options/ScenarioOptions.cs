namespace RevitHelperBot.Application.Options;

public sealed class ScenarioOptions
{
    public const string SectionName = "Scenario";

    public string FilePath { get; init; } = "data/scenario.json";
}
