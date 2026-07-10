using InventoryManagement.Infrastructure.Identity;

namespace InventoryManagement.API.Extensions
{
    public static class IdentityBootstrapExtensions
    {
        public static async Task<WebApplication> BootstrapIdentityAsync(
            this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var bootstrapService = scope.ServiceProvider
                .GetRequiredService<IdentityBootstrapService>();

            await bootstrapService.BootstrapAsync();

            return app;
        }
    }
}
