using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StayHub.Common.Extensions;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddStayHubCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName = "AllowFrontend")
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
                policy.WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        return services;
    }
}
