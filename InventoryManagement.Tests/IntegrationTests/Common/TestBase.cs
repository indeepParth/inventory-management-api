using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Tests.IntegrationTests.Common
{
    public abstract class TestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;

        protected TestBase(CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();
        }
    }
}