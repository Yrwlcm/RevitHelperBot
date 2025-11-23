using Microsoft.Extensions.Options;
using RevitHelperBot.Api.Options;
using RevitHelperBot.Api.Services;
using RevitHelperBot.Application;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();

builder.Services.AddOptions<TelegramBotOptions>()
    .Bind(builder.Configuration.GetSection(TelegramBotOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BotToken),
        "Telegram bot token must be provided via configuration or environment variable.")
    .ValidateOnStart();

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<ScenarioOptions>(builder.Configuration.GetSection(ScenarioOptions.SectionName));

builder.Services.AddHttpClient(TelegramBotOptions.HttpClientName)
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var options = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
        return new TelegramBotClient(options.BotToken!, httpClient);
    });

builder.Services.AddScoped<IBotResponseSender, TelegramBotResponseSender>();
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok("RevitHelperBot API is running"));

app.Run();
