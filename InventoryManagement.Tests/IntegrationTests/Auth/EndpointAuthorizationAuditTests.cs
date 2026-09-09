using System.Net;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class EndpointAuthorizationAuditTests : TestBase
    {
        public EndpointAuthorizationAuditTests(
            CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Theory]
        [InlineData("/api/products")]
        [InlineData("/api/categories")]
        [InlineData("/api/units")]
        [InlineData("/api/customers")]
        [InlineData("/api/suppliers")]
        [InlineData("/api/drivers")]
        [InlineData("/api/company-profile")]
        [InlineData("/api/company-invitations")]
        [InlineData("/api/users")]
        [InlineData("/api/purchases")]
        [InlineData("/api/sales-invoices")]
        [InlineData("/api/delivery-challans")]
        [InlineData("/api/payments")]
        [InlineData("/api/stock-movements")]
        [InlineData("/api/inventory-reports/current-stock")]
        [InlineData("/api/companies/current/access")]
        public async Task Company_Scoped_Get_Endpoints_Should_Require_Active_Company(
            string path)
        {
            await AuthenticateAsync();
            Client.DefaultRequestHeaders.Remove("X-Company-Id");

            var response = await Client.GetAsync(path);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
