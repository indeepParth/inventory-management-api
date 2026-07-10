using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace InventoryManagement.API.Configuration
{
    public static class RateLimitingConfiguration
    {
        public static void Configure(
            RateLimiterOptions options,
            ApiRateLimitOptions rateLimitOptions)
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => CreateLimiterOptions(rateLimitOptions.Global)));

            options.AddPolicy(
                RateLimitPolicyNames.Login,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => CreateLimiterOptions(rateLimitOptions.Login)));

            options.AddPolicy(
                RateLimitPolicyNames.Register,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => CreateLimiterOptions(rateLimitOptions.Register)));

            options.AddPolicy(
                RateLimitPolicyNames.RefreshToken,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => CreateLimiterOptions(rateLimitOptions.RefreshToken)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds)
                            .ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.StatusCode =
                    StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    message = "Too many requests.",
                    traceId = context.HttpContext.TraceIdentifier
                };

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(response),
                    cancellationToken);
            };
        }

        private static FixedWindowRateLimiterOptions CreateLimiterOptions(
            FixedWindowRateLimitOptions options)
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.SafePermitLimit,
                Window = TimeSpan.FromSeconds(options.SafeWindowSeconds),
                QueueLimit = options.SafeQueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            };
        }

        private static string GetClientPartitionKey(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            var userName = context.User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                return $"user:{userName}";
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                return $"ip:{remoteIp}";
            }

            return "ip:unknown";
        }
    }
}
