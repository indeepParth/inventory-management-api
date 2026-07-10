using System.Net;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HealthEndpointTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Live_Should_Be_Available_Without_Authentication()
        {
            var response = await _client.GetAsync("/health/live");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("\"status\":\"Healthy\"");
            body.Should().Contain("\"traceId\":");
            body.Should().NotContain("Data Source");
            body.Should().NotContain("ConnectionStrings");
            body.Should().NotContain("Exception");
        }

        [Fact]
        public async Task Ready_Should_Verify_Database_And_Be_Available_Without_Authentication()
        {
            var response = await _client.GetAsync("/health/ready");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("\"status\":\"Healthy\"");
            body.Should().Contain("\"traceId\":");
            body.Should().NotContain("Data Source");
            body.Should().NotContain("ConnectionStrings");
            body.Should().NotContain("Exception");
            body.Should().NotContain("database");
        }

        [Fact]
        public async Task Business_Endpoint_Should_Still_Require_Authentication()
        {
            var response = await _client.GetAsync("/api/products");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
