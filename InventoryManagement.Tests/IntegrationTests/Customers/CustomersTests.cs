using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Customers;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Tests.IntegrationTests.Common;
using CreateCustomerCommand = InventoryManagement.Application.Features.Customers.CreateCustomer.Command;
using UpdateCustomerCommand = InventoryManagement.Application.Features.Customers.UpdateCustomer.Command;

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

        [Fact]
        public async Task GetCustomers_Should_Page_Search_And_Filter()
        {
            await AuthenticateAsync();
            var matching = ValidCommand();
            matching.Name = $"Search target {Guid.NewGuid():N}";
            matching.Phone = "5550001234";
            matching.GstNumber = null;
            var other = ValidCommand();
            other.Name = $"Other {Guid.NewGuid():N}";
            other.GstNumber = null;

            await Client.PostAsJsonAsync("/api/customers", matching);
            await Client.PostAsJsonAsync("/api/customers", other);

            var response = await Client.GetAsync(
                "/api/customers?pageNumber=1&pageSize=1&search=5550001234&isActive=true");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x => x.Name == matching.Name);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Update_Should_Edit_Customer_And_Preserve_CreatedAtUtc()
        {
            await AuthenticateAsync();
            var createResponse = await Client.PostAsJsonAsync("/api/customers", ValidCommand());
            var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            created.Should().NotBeNull();

            var update = new UpdateCustomerCommand(
                0,
                $"Updated {Guid.NewGuid():N}",
                "Updated contact",
                "9999999999",
                "updated@example.com",
                "New billing address",
                "New delivery address",
                created!.GstNumber,
                75000,
                false);

            var response = await Client.PutAsJsonAsync($"/api/customers/{created.Id}", update);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<CustomerResponse>();
            updated.Should().NotBeNull();
            updated!.Name.Should().Be(update.Name);
            updated.IsActive.Should().BeFalse();
            updated.CreatedAtUtc.Should().Be(created.CreatedAtUtc);
            updated.UpdatedAtUtc.Should().BeAfter(created.UpdatedAtUtc);
        }

        [Fact]
        public async Task Update_Should_Reject_Duplicate_Name_And_Gstin()
        {
            await AuthenticateAsync();
            var first = await CreateCustomerAsync();
            var second = await CreateCustomerAsync();

            var duplicateName = ToUpdate(second, first.Name, second.GstNumber);
            (await Client.PutAsJsonAsync($"/api/customers/{second.Id}", duplicateName))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var duplicateGst = ToUpdate(second, second.Name, first.GstNumber);
            (await Client.PutAsJsonAsync($"/api/customers/{second.Id}", duplicateGst))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Deactivate_Should_Be_Idempotent_And_Keep_Customer_Queryable()
        {
            await AuthenticateAsync();
            var customer = await CreateCustomerAsync();

            var firstResponse = await Client.PatchAsync(
                $"/api/customers/{customer.Id}/deactivate",
                null);
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var firstResult = await firstResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            firstResult.Should().NotBeNull();
            firstResult!.IsActive.Should().BeFalse();

            var secondResponse = await Client.PatchAsync(
                $"/api/customers/{customer.Id}/deactivate",
                null);
            secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var secondResult = await secondResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            secondResult.Should().NotBeNull();
            secondResult!.IsActive.Should().BeFalse();
            secondResult.UpdatedAtUtc.Should().Be(firstResult.UpdatedAtUtc);

            var getResponse = await Client.GetAsync($"/api/customers/{customer.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            fetched.Should().NotBeNull();
            fetched!.IsActive.Should().BeFalse();

            var listResponse = await Client.GetAsync(
                $"/api/customers?search={Uri.EscapeDataString(customer.Name)}&isActive=false");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await listResponse.Content
                .ReadFromJsonAsync<PagedResponse<CustomerResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x => x.Id == customer.Id);
        }

        private async Task<CustomerResponse> CreateCustomerAsync()
        {
            var response = await Client.PostAsJsonAsync("/api/customers", ValidCommand());
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
        }

        private static UpdateCustomerCommand ToUpdate(
            CustomerResponse customer,
            string name,
            string? gstNumber)
        {
            return new UpdateCustomerCommand(
                customer.Id,
                name,
                customer.ContactPerson,
                customer.Phone,
                customer.Email,
                customer.BillingAddress,
                customer.DeliveryAddress,
                gstNumber,
                customer.CreditLimit,
                customer.IsActive);
        }

        private static CreateCustomerCommand ValidCommand() => new()
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
