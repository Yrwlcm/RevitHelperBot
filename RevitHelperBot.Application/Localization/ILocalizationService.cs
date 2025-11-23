using RevitHelperBot.Application.Conversation;

namespace RevitHelperBot.Application.Localization;

public interface ILocalizationService
{
    string WelcomeMessage { get; }

    string FormatEcho(string message, ConversationState state);
}
