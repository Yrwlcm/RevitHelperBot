namespace RevitHelperBot.Application.Documents;

public sealed record DocumentSearchHit(string RelativePath, bool PhraseMatch, IReadOnlyList<string> Contexts);
