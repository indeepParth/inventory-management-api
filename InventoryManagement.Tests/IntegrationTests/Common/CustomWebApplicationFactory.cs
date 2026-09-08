using InventoryManagement.Application.Common.Options;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Tests.IntegrationTests.Common
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly PostgresTestDatabase _database = new();

        public CustomWebApplicationFactory()
        {
            
        }

        protected override void ConfigureWebHost(
        IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = new string('T', JwtOptions.MinimumSigningKeyBytes)
                    });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Register test database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_database.ConnectionString);
                });

                var serviceProvider = services.BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _database.CreateAsync().GetAwaiter().GetResult();
                db.Database.Migrate();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _database.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}
