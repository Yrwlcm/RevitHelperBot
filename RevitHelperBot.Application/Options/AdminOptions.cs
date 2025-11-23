namespace RevitHelperBot.Application.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public List<long> AllowedUserIds { get; init; } = new();
}
