using System.Text.Json;
using FluentAssertions;
using InventoryManagement.API.Logging;
using InventoryManagement.API.Middlewares;
using InventoryManagement.Application.Common.Behaviors;
using InventoryManagement.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class LoggingHardeningTests
    {
        [Theory]
        [InlineData("Authentication failed with {Authorization}")]
        [InlineData("Refresh failed for {RefreshToken}")]
        [InlineData("Payment reversed with {ExternalReference}")]
        [InlineData("Configured secret {JwtKey}")]
        public void SensitiveDataLogFilter_Should_Reject_Sensitive_Log_Properties(
            string messageTemplate)
        {
            var filter = new SensitiveDataLogFilter();
            var parser = new MessageTemplateParser();
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                parser.Parse(messageTemplate),
                []);

            var enabled = filter.IsEnabled(logEvent);

            enabled.Should().BeFalse();
        }

        [Theory]
        [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signaturevalue")]
        [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signaturevalue")]
        public void SensitiveDataLogFilter_Should_Reject_Sensitive_Log_Values(
            string sensitiveValue)
        {
            var filter = new SensitiveDataLogFilter();
            var parser = new MessageTemplateParser();
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                parser.Parse("Header value {Header}"),
                [
                    new LogEventProperty(
                        "Header",
                        new ScalarValue(sensitiveValue))
                ]);

            var enabled = filter.IsEnabled(logEvent);

            enabled.Should().BeFalse();
        }

        [Fact]
        public void SensitiveDataLogFilter_Should_Allow_Safe_Transition_Log()
        {
            var filter = new SensitiveDataLogFilter();
            var parser = new MessageTemplateParser();
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                parser.Parse("Business transition completed: {BusinessTransition}"),
                [
                    new LogEventProperty(
                        "BusinessTransition",
                        new ScalarValue("PostPurchase"))
                ]);

            var enabled = filter.IsEnabled(logEvent);

            enabled.Should().BeTrue();
        }

        [Fact]
        public void BusinessTransitionLogging_Should_Recognize_Only_Transition_Commands()
        {
            BusinessTransitionLoggingBehavior<object, object>
                .TryGetBusinessTransition(
                    "InventoryManagement.Application.Features.Purchases.PostPurchase.Command",
                    out var transitionName)
                .Should()
                .BeTrue();

            transitionName.Should().Be("PostPurchase");

            BusinessTransitionLoggingBehavior<object, object>
                .TryGetBusinessTransition(
                    "InventoryManagement.Application.Features.Auth.Login.Command",
                    out _)
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_Should_Not_Log_Exception_For_Validation_Failure()
        {
            var logger = new TestLogger<GlobalExceptionMiddleware>();
            var middleware = new GlobalExceptionMiddleware(
                _ => throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["Password"] = ["Password is required."]
                    }),
                logger);
            var context = new DefaultHttpContext
            {
                Response =
                {
                    Body = new MemoryStream()
                }
            };

            await middleware.InvokeAsync(context);

            logger.Entries.Should().ContainSingle();
            logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
            logger.Entries[0].Exception.Should().BeNull();

            context.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(context.Response.Body);
            document.RootElement.GetProperty("traceId").GetString()
                .Should()
                .Be(context.TraceIdentifier);
        }

        private sealed class TestLogger<T> : ILogger<T>
        {
            public List<LogEntry> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add(new LogEntry(logLevel, exception));
            }
        }

        private sealed record LogEntry(
            LogLevel LogLevel,
            Exception? Exception);
    }
}
