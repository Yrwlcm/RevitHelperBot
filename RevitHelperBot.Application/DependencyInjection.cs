using Microsoft.Extensions.DependencyInjection;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Interfaces;

namespace RevitHelperBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IConversationStateStore, InMemoryConversationStateStore>();
        services.AddScoped<IConversationEngine, ConversationEngine>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddSingleton<IScenarioRepository, JsonScenarioRepository>();
        services.AddSingleton<IScenarioService, ScenarioService>();
        services.AddSingleton<IWordDocumentsRepository, FileSystemWordDocumentsRepository>();
        services.AddSingleton<IDocxTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IDocumentSearchService, DocumentSearchService>();
        services.AddSingleton<IDocumentSearchResultFormatter, DocumentSearchResultFormatter>();
        services.AddScoped<IBotResponseSender, NoOpBotResponseSender>();
        services.AddScoped<IBotUpdateService, BotUpdateService>();
        return services;
    }
}
