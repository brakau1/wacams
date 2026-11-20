using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Wacam.Api.Data;
using Wacam.Api.Data.SeedData;
using Wacam.Api.Extensions;
using Wacam.Api.Middleware;
using Wacam.Api.Policies;
using Wacam.Api.Services;
using WACAMS.Core.Interfaces;
using WACAMS.Core.Disciplinary.Services;
using WACAMS.Infrastructure.Services;
using WACAMS.Infrastructure.Services.NotificationChannels;

namespace Wacam.Api;

/// <summary>
/// Application startup — DI, middleware pipeline, auth, CORS, Swagger, health checks.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Serilog configuration ──
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/wacam-.txt", rollingEnabled: true, fileSizeLimitBytes: 10_485_760, retainedFileCountLimit: 30)
            .CreateLogger();

        builder.Host.UseSerilog();

        // ── Database ──
        var connString = builder.Configuration.GetConnectionString("WacamDb")
            ?? throw new InvalidOperationException("Connection string 'WacamDb' is not configured.");

        builder.Services.AddDbContext<WacamDbContext>(options =>
            options.UseNpgsql(connString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null)));

        // ── JWT Authentication ──
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? "WACAMS-Default-JWT-Secret-Key-MustBe-At-Least-32-Characters!";
        var jwtIssuer = jwtSection["Issuer"] ?? "WACAMS";
        var jwtAudience = jwtSection["Audience"] ?? "WACAMS-API";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning("JWT authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Log.Information("JWT validated for employee: {EmployeeId}",
                            context.Principal?.FindFirst("employeeId")?.Value ?? "unknown");
                        return Task.CompletedTask;
                    }
                };
            });

        // Authorization policies registered below with notification platform config

        // ── Controllers & Validation ──
        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<ValidateModelAttribute>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase for API
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        // ── Swagger / OpenAPI ──
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WACAMS API",
                Version = "v1",
                Description = "Workforce Attendance & Corrective Action Management System — Production-ready REST API",
                Contact = new OpenApiContact { Name = "Curi Lab", Url = new Uri("https://qgao2.com") }
            });

            // Include XML comments
            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);

            // JWT Bearer scheme in Swagger UI
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer [token]'.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ── Health Checks ──
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WacamDbContext>("Database");

        // ── Register API Services ──
        builder.Services.AddApiServices();

        // ── RBAC Services ──
        builder.Services.AddScoped<PermissionService>();

        // ── Department Authorization Service ──
        builder.Services.AddScoped<DepartmentAuthorizationService>();

        // ── Admin User Service ──
        builder.Services.AddScoped<AdminUserService>();

        // ── SuperAdmin Service ──
        builder.Services.AddScoped<SuperAdminService>();

        // ── SuperAdmin Enhancement Pack Services ──
        builder.Services.AddScoped<PermissionTemplateService>();
        builder.Services.AddScoped<SecurityOperationsService>();
        builder.Services.AddScoped<BulkAdminService>();
        builder.Services.AddScoped<AccessReviewService>();
        builder.Services.AddScoped<SecurityAnalyticsService>();
        builder.Services.AddScoped<GISAdminService>();

        // ── Notification Platform ──
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Notification:Email"));
        builder.Services.Configure<TeamsSettings>(builder.Configuration.GetSection("Notification:Teams"));
        builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("Notification:SMS"));
        builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("Notification:WhatsApp"));

        builder.Services.AddSingleton<TemplateEngine>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        builder.Services.AddScoped<INotificationChannel, TeamsNotificationChannel>();
        builder.Services.AddScoped<INotificationChannel, SmsNotificationChannel>();
        builder.Services.AddScoped<INotificationChannel, WhatsAppNotificationChannel>();
        builder.Services.AddHttpClient<TeamsNotificationChannel>();
        builder.Services.AddHttpClient<SmsNotificationChannel>();
        builder.Services.AddHttpClient<WhatsAppNotificationChannel>();

        // ── Authorization Policies ──
        builder.Services.AddAuthorization(options =>
        {
            AuthorizationPolicies.Configure(options);
        });

        // ── CORS ──
        builder.Services.ConfigureCors(builder.Environment);

        // ── File uploads ──
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
        });

        // ── Background Jobs ──
        builder.Services.AddHostedService<Wacam.Api.Jobs.EscalationMonitorJob>();
        builder.Services.AddHostedService<Wacam.Api.Jobs.AttendanceExceptionMonitorJob>();
        builder.Services.AddHostedService<Wacam.Api.Jobs.CorrectiveActionExpiryJob>();
        builder.Services.AddHostedService<Wacam.Api.Jobs.AttendanceKpiRefreshJob>();
        builder.Services.AddHostedService<Wacam.Api.Jobs.ExecutiveMetricsRefreshJob>();
        builder.Services.AddHostedService<Wacam.Api.Jobs.NotificationDispatcherJob>();

        // ── Analytics Services ──
        builder.Services.AddScoped<DashboardAggregationService>();
        builder.Services.AddScoped<TrendAnalyticsService>();
        builder.Services.AddScoped<ExecutiveAnalyticsService>();
        builder.Services.AddScoped<AttendanceAnalyticsService>();
        builder.Services.AddScoped<CorrectiveActionAnalyticsService>();
        builder.Services.AddScoped<EscalationAnalyticsService>();
        builder.Services.AddScoped<DepartmentAnalyticsService>();
        builder.Services.AddScoped<PlantPerformanceService>();

        // ── GIS Integration Services (Phase 4A) ──
        builder.Services.AddScoped<EncryptionService>();
        builder.Services.AddScoped<GISAuthenticationService>();
        builder.Services.AddHttpClient("GISClient");

        var app = builder.Build();

        // ── Seed RBAC Data ──
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
            await PermissionSeeder.SeedAsync(context);
            await NotificationTemplateSeeder.SeedAsync(context);
            await AdminUserSeeder.SeedAsync(context);
        }

        // ── Middleware Pipeline (order is critical) ──

        // 1. Exception Handling Middleware
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 2. HTTPS redirection (production only)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // 3. CORS
        app.UseCors("WacamCors");

        // 4. Authentication (JWT Bearer)
        app.UseAuthentication();

        // 4.5. DEV-ONLY: Bypass authentication — inject fake development user
        if (app.Environment.IsDevelopment())
        {
            app.UseMiddleware<Api.Middleware.DevelopmentUserMiddleware>();
        }

        // 5. Authorization
        app.UseAuthorization();

        // 6. Audit Log Middleware (after auth so we have user identity)
        app.UseMiddleware<AuditLogMiddleware>();

        // 7. Request/Response Logging (Serilog handles this via middleware)
        app.UseSerilogRequestLogging();

        // 8. Routing
        app.MapControllers();

        // 9. Endpoint Mapping — Swagger (development only)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WACAMS API v1");
                c.RoutePrefix = string.Empty; // Serve at root
            });
        }

        // 10. Health check endpoint
        app.MapHealthChecks("/api/health");

        // 11. Static files for uploaded signed forms
        app.UseStaticFiles();

        Log.Information("WACAMS API starting on environment: {Environment}", app.Environment.EnvironmentName);
        app.Run();
    }
}
