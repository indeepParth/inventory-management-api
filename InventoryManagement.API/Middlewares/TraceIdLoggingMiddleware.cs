using Serilog.Context;

namespace InventoryManagement.API.Middlewares
{
    public sealed class TraceIdLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public TraceIdLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            {
                await _next(context);
            }
        }
    }
}
