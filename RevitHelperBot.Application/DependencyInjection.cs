using Microsoft.Extensions.DependencyInjection;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Interfaces;

namespace RevitHelperBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBotUpdateService, BotUpdateService>();
        return services;
    }
}
