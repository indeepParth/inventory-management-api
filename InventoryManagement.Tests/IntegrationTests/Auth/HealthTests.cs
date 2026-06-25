using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class HealthTests : TestBase
    {
        public HealthTests(CustomWebApplicationFactory factory) : base(factory)
        {

        }

        [Fact]
        public async Task Swagger_Should_Be_Available()
        {
            // Act
            var response = await Client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }
}