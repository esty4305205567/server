using Microsoft.OpenApi.Models;

namespace GameHub.API.Infrastructure;

public static class SwaggerServiceCollectionExtensions
{
    private const string SchemeName = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "יש להדביק את הטוקן בלבד, בלי המילה Bearer",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = SchemeName
                }
            };

            options.AddSecurityDefinition(SchemeName, scheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { scheme, Array.Empty<string>() }
            });
        });

        return services;
    }
}