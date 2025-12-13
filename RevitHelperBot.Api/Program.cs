using RevitHelperBot.Application;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddControllers();

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<ScenarioOptions>(builder.Configuration.GetSection(ScenarioOptions.SectionName));
builder.Services.Configure<DocumentsOptions>(builder.Configuration.GetSection(DocumentsOptions.SectionName));

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<SimulationRunner>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
