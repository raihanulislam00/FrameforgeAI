using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Application.Services;
using VideoMaker.Application.Validation;

namespace VideoMaker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAiChatService, AiChatService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IVideoService, VideoService>();

        return services;
    }
}
