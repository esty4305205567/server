using GameHub.Core.Interfaces;
using GameHub.Service;
using GameHub.Service.Mapping;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(GameSessionMappingProfile).Assembly);

        services.AddSingleton<Random>(_ => Random.Shared);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IGameCodeGenerator, GameCodeGenerator>();
        services.AddSingleton<ILootTiming, LootTiming>();

        services.AddSingleton<IGameItemGenerator, LootItemGenerator>();
        services.AddSingleton<IGameItemGenerator, NumberRaceItemGenerator>();
        services.AddSingleton<IGameItemGenerator, QuizItemGenerator>();

        services.AddSingleton<IGameSessionConfigurator, LootSessionConfigurator>();

        services.AddSingleton<ICaptureRule, LootCaptureRule>();
        services.AddSingleton<ICaptureRule, NumberRaceCaptureRule>();
        services.AddSingleton<ICaptureRule, QuizCaptureRule>();

        services.AddScoped<IGameFinisher, GameFinisher>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IGameCaptureService, GameCaptureService>();
        services.AddScoped<IGameStateService, GameStateService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameSettingService, GameSettingService>();
        services.AddScoped<IGameSettingService, GameSettingService>();
        services.AddScoped<IAdminGameSessionService, AdminGameSessionService>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IGameSettingService, GameSettingService>();
        services.AddScoped<IAdminGameSessionService, AdminGameSessionService>();
        services.AddScoped<IGameHistoryService, GameHistoryService>();
        return services;

    }
}