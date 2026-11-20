using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wacam.Api.Data;

namespace WACAMS.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that configures an in-memory SQLite database
/// for integration testing against the WACAMS API.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"wacam_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext registrations regardless of type
            var descriptors = services.Where(d =>
                d.ImplementationType == typeof(WacamDbContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
                .ToList();

            foreach (var desc in descriptors)
                services.Remove(desc);

            // Register in-memory SQLite DbContext
            services.AddDbContext<WacamDbContext>(options =>
                options.UseSqlite($"Data Source={_databaseName}.db;Cache=Shared"));

            // Build service provider to ensure all services are registered
            var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false });

            // Ensure database is created and migrated
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
            try
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();
            }
            finally
            {
                context.Database.CloseConnection();
            }

            // Seed test data
            TestDataSeeder.SeedTestData(context);
        });
    }
}
