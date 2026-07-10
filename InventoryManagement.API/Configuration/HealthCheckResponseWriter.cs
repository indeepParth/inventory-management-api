using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InventoryManagement.API.Configuration
{
    public static class HealthCheckResponseWriter
    {
        public static async Task WriteAsync(
            HttpContext context,
            HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
        }
    }
}
