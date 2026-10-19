using GameHub.Core.Interfaces;
using GameHub.Core.Options;
using GameHub.Data;
using GameHub.Data.Repositories;
using GameHub.Data.Security;
using GameHub.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "חסר ConnectionStrings:Default. יש לבדוק את ה-User Secrets של GameHub.API.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<SeedOptions>(
            configuration.GetSection(SeedOptions.SectionName));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<IGameItemRepository, GameItemRepository>();
        services.AddScoped<IGameSettingRepository, GameSettingRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}