using Microsoft.Extensions.Options;
using RevitHelperBot.Api.Contracts;
using RevitHelperBot.Api.Options;
using RevitHelperBot.Api.Services;
using RevitHelperBot.Application;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Entities;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<ScenarioOptions>(builder.Configuration.GetSection(ScenarioOptions.SectionName));

builder.Services.Configure<TelegramBotOptions>(builder.Configuration.GetSection(TelegramBotOptions.SectionName));

var telegramOptions = builder.Configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
if (!string.IsNullOrWhiteSpace(telegramOptions?.BotToken))
{
    builder.Services.AddHttpClient(TelegramBotOptions.HttpClientName)
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
            return new TelegramBotClient(options.BotToken!, httpClient);
        });

    builder.Services.AddScoped<IBotResponseSender, TelegramBotResponseSender>();
    builder.Services.AddHostedService<TelegramBotService>();
}

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");
app.MapPost("/simulate", async (
        SimulateRequest request,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken) =>
    {
        if (request is null)
        {
            return Results.BadRequest("Request body is required.");
        }

        using var scope = scopeFactory.CreateScope();
        var stateStore = scope.ServiceProvider.GetRequiredService<IConversationStateStore>();
        var localization = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        var scenarioService = scope.ServiceProvider.GetRequiredService<IScenarioService>();
        var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BotUpdateService>>();

        var responseSender = new CapturingBotResponseSender();
        var engine = new ConversationEngine(stateStore, localization, responseSender, scenarioService);
        var botUpdateService = new BotUpdateService(engine, scenarioService, responseSender, adminOptions, logger);

        var command = ExtractCommand(request.Text);
        var senderId = request.SenderId ?? request.ChatId;
        var update = new BotUpdate(
            request.ChatId,
            senderId,
            request.Username ?? "web-user",
            request.Text,
            command,
            request.CallbackData);

        await botUpdateService.HandleUpdateAsync(update, cancellationToken);

        var messages = responseSender.Responses.Select(r => new SimulateMessage(
                r.Text,
                r.ImageUrl,
                r.Buttons?.Select(b => new SimulateButton(b.Text, b.NextNodeId)).ToList() ?? new List<SimulateButton>()))
            .ToList();

        return Results.Ok(new SimulateResponse(messages));
    });

app.Run();

static string? ExtractCommand(string? messageText)
{
    if (string.IsNullOrWhiteSpace(messageText))
    {
        return null;
    }

    if (messageText.StartsWith("/"))
    {
        var command = messageText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return command?.ToLowerInvariant();
    }

    return null;
}
