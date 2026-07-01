using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Customers;
using InventoryManagement.Application.Features.Customers.CreateCustomer;
using InventoryManagement.Tests.IntegrationTests.Common;

namespace InventoryManagement.Tests.IntegrationTests.Customers
{
    public class CustomersTests : TestBase
    {
        public CustomersTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Customer_Endpoints_Should_Require_Authentication()
        {
            Client.DefaultRequestHeaders.Authorization = null;

            var postResponse = await Client.PostAsJsonAsync("/api/customers", ValidCommand());
            var getResponse = await Client.GetAsync("/api/customers/1");

            postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_Then_Get_Should_Return_Customer()
        {
            await AuthenticateAsync();
            var command = ValidCommand();

            var createResponse = await Client.PostAsJsonAsync("/api/customers", command);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            created.Should().NotBeNull();
            created!.Name.Should().Be(command.Name);
            created.GstNumber.Should().Be(command.GstNumber);
            created.CreditLimit.Should().Be(command.CreditLimit);
            created.IsActive.Should().BeTrue();
            created.CreatedAtUtc.Should().Be(created.UpdatedAtUtc);

            var getResponse = await Client.GetAsync($"/api/customers/{created.Id}");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            fetched.Should().BeEquivalentTo(created);
        }

        [Fact]
        public async Task Create_Should_Reject_Case_Insensitive_Duplicate_Name()
        {
            await AuthenticateAsync();
            var name = $"Customer {Guid.NewGuid():N}";
            var first = ValidCommand();
            first.Name = name;
            first.GstNumber = null;
            var duplicate = ValidCommand();
            duplicate.Name = name.ToUpperInvariant();
            duplicate.GstNumber = null;

            (await Client.PostAsJsonAsync("/api/customers", first))
                .StatusCode.Should().Be(HttpStatusCode.Created);
            (await Client.PostAsJsonAsync("/api/customers", duplicate))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_Should_Reject_Duplicate_Or_Invalid_Gstin()
        {
            await AuthenticateAsync();
            var gstNumber = "27AAPFU0939F1ZV";
            var first = ValidCommand();
            first.Name = $"First {Guid.NewGuid():N}";
            first.GstNumber = gstNumber;
            var duplicate = ValidCommand();
            duplicate.Name = $"Second {Guid.NewGuid():N}";
            duplicate.GstNumber = gstNumber.ToLowerInvariant();

            (await Client.PostAsJsonAsync("/api/customers", first))
                .StatusCode.Should().Be(HttpStatusCode.Created);
            (await Client.PostAsJsonAsync("/api/customers", duplicate))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var invalid = ValidCommand();
            invalid.Name = $"Invalid {Guid.NewGuid():N}";
            invalid.GstNumber = "invalid";
            (await Client.PostAsJsonAsync("/api/customers", invalid))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static Command ValidCommand() => new()
        {
            Name = $"Customer {Guid.NewGuid():N}",
            ContactPerson = "Priya Shah",
            Phone = "9876543210",
            Email = "priya@example.com",
            BillingAddress = "Mumbai",
            DeliveryAddress = "Pune",
            GstNumber = $"27AAPFU{Random.Shared.Next(1000, 9999)}F1ZV",
            CreditLimit = 50000
        };
    }
}
