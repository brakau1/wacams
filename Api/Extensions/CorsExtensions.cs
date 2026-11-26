namespace WACAMS.Api.Extensions;

/// <summary>
/// CORS policy configuration — development allows all origins, production restricts to approved domains.
/// </summary>
public static class CorsExtensions
{
    public static IServiceCollection ConfigureCors(this IServiceCollection services, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            // Development: allow all origins for convenience
            services.AddCors(options =>
            {
                options.AddPolicy("WacamCors", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
        }
        else
        {
            // Production: restrict to specific origins
            var allowedOrigins = new List<string>
            {
                "https://wacam.yfpo.com",
                "https://wacam.yfpo.co.za"
            };

            // Also read from config if available
            var originsFromConfig = Environment.GetEnvironmentVariable("WACAMS_ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(originsFromConfig))
            {
                allowedOrigins.AddRange(originsFromConfig.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            services.AddCors(options =>
            {
                options.AddPolicy("WacamCors", policy =>
                {
                    policy.WithOrigins(allowedOrigins.ToArray())
                          .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                          .WithHeaders("content-type", "authorization", "accept", "x-requested-with")
                          .AllowCredentials();
                });
            });
        }

        return services;
    }
}
