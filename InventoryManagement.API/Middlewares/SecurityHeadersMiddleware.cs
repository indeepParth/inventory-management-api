namespace InventoryManagement.API.Middlewares
{
    public sealed class SecurityHeadersMiddleware
    {
        private const string ApiContentSecurityPolicy =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

        private const string SwaggerContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
            "font-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; " +
            "base-uri 'self'; form-action 'none'";

        private readonly RequestDelegate _next;
        private readonly bool _swaggerEnabled;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            bool swaggerEnabled)
        {
            _next = next;
            _swaggerEnabled = swaggerEnabled;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = _swaggerEnabled
                ? SwaggerContentSecurityPolicy
                : ApiContentSecurityPolicy;

            await _next(context);
        }
    }
}
