using RevitHelperBot.Application.Conversation;

namespace RevitHelperBot.Application.Localization;

public class LocalizationService : ILocalizationService
{
    public string WelcomeMessage => "System Online";

    public string FormatEcho(string message, ConversationState state)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message;
    }
}
