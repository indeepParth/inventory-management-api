using FluentAssertions;
using InventoryManagement.API.Middlewares;
using Microsoft.AspNetCore.Http;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class HttpSecurityHeadersTests
    {
        [Fact]
        public async Task Middleware_Should_Add_Api_Security_Headers()
        {
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(
                WriteEmptyResponseAsync,
                swaggerEnabled: false);

            await middleware.InvokeAsync(context);

            context.Response.Headers["X-Content-Type-Options"].ToString()
                .Should()
                .Be("nosniff");
            context.Response.Headers["X-Frame-Options"].ToString()
                .Should()
                .Be("DENY");
            context.Response.Headers["Referrer-Policy"].ToString()
                .Should()
                .Be("no-referrer");
            context.Response.Headers["Content-Security-Policy"].ToString()
                .Should()
                .Be("default-src 'none'; frame-ancestors 'none'; base-uri 'none'");
        }

        [Fact]
        public async Task Middleware_Should_Use_Swagger_Compatible_Csp_When_Swagger_Is_Enabled()
        {
            var context = CreateHttpContext();
            var middleware = new SecurityHeadersMiddleware(
                WriteEmptyResponseAsync,
                swaggerEnabled: true);

            await middleware.InvokeAsync(context);

            var contentSecurityPolicy =
                context.Response.Headers["Content-Security-Policy"].ToString();

            contentSecurityPolicy.Should().Contain("default-src 'self'");
            contentSecurityPolicy.Should().Contain("script-src 'self' 'unsafe-inline'");
            contentSecurityPolicy.Should().Contain("style-src 'self' 'unsafe-inline'");
            contentSecurityPolicy.Should().Contain("frame-ancestors 'none'");
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext
            {
                Response =
                {
                    Body = new MemoryStream()
                }
            };
        }

        private static async Task WriteEmptyResponseAsync(HttpContext context)
        {
            await context.Response.WriteAsync(string.Empty);
        }
    }
}
