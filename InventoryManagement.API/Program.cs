using InventoryManagement.API;
using InventoryManagement.API.Configuration;
using InventoryManagement.API.Extensions;
using InventoryManagement.API.Middlewares;
using InventoryManagement.Application;
using InventoryManagement.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;


var builder = WebApplication.CreateBuilder(args);

StartupConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment);

builder.AddLoggingServices();

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();
var swaggerEnabled = SwaggerConfiguration.IsEnabled(
    app.Configuration,
    app.Environment);

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.ApplyConfiguredMigrationsAsync();
    await app.BootstrapIdentityAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>(swaggerEnabled);
app.UseMiddleware<TraceIdLoggingMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, exception) =>
        exception is not null ||
        httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors(CorsPolicyNames.Frontend);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        })
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        })
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapControllers();

app.Run();

public partial class Program
{
    
}
