using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryManagement.Tests.IntegrationTests.Common
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
        IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}