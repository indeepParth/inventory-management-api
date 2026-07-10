
using InventoryManagement.API.Logging;
using Serilog;

namespace InventoryManagement.API.Extensions
{
    public static class LoggingExtensions
    {
        public static WebApplicationBuilder AddLoggingServices(this WebApplicationBuilder builder)
        {
            // Serilog configuration here
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Filter.With(new SensitiveDataLogFilter())
            .CreateLogger();

            builder.Host.UseSerilog();

            return builder;
        }
    }
}
