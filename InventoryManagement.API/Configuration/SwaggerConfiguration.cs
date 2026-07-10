namespace InventoryManagement.API.Configuration
{
    public static class SwaggerConfiguration
    {
        public static bool IsEnabled(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            return !environment.IsProduction() ||
                   configuration.GetValue<bool>("Swagger:Enabled");
        }
    }
}
