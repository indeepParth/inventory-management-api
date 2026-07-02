using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.CustomerReturns;
using InventoryManagement.Application.Features.CustomerReturns.CreateCustomerReturn;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreateInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.Command;
using CreateInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.SalesInvoiceItemInput;

namespace InventoryManagement.Tests.IntegrationTests.CustomerReturns
{
    public class CustomerReturnEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public CustomerReturnEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Post_And_Cancel_Should_Reverse_Stock_Account_And_History()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var invoice = await CreateAndPostInvoiceAsync(seed);

            var create = await Client.PostAsJsonAsync(
                "/api/customer-returns",
                new Command
                {
                    ReturnNumber = $"RET-{Guid.NewGuid():N}",
                    SalesInvoiceId = invoice.Id,
                    ReturnDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new CustomerReturnItemInput
                        {
                            SalesInvoiceItemId = invoice.Items[0].Id,
                            Quantity = 2
                        }
                    }
                });

            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var draft = await create.Content
                .ReadFromJsonAsync<CustomerReturnResponse>();
            draft.Should().NotBeNull();
            draft!.Status.Should().Be(CustomerReturnStatus.Draft);
            draft.Subtotal.Should().Be(80);
            draft.TaxAmount.Should().Be(14.40m);
            draft.GrandTotal.Should().Be(94.40m);
            draft.Items[0].SellingUnitPrice.Should().Be(40);
            draft.Items[0].TaxRate.Should().Be(18);
            draft.Items[0].CostAtSale.Should().Be(25);

            var post = await Client.PostAsync(
                $"/api/customer-returns/{draft.Id}/post",
                null);
            post.StatusCode.Should().Be(HttpStatusCode.OK);
            var repeatedPost = await Client.PostAsync(
                $"/api/customer-returns/{draft.Id}/post",
                null);
            repeatedPost.StatusCode.Should().Be(HttpStatusCode.OK);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                (await db.Products.SingleAsync(x => x.Id == seed.ProductId))
                    .Quantity.Should().Be(9);
                (await db.Customers.SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue.Should().Be(47.20m);
                var movement = await db.StockMovements.SingleAsync(x =>
                    x.SourceType == "CustomerReturn" &&
                    x.SourceId == draft.Id.ToString());
                movement.MovementType.Should()
                    .Be(StockMovementType.CustomerReturn);
                movement.QuantityChange.Should().Be(2);
                movement.UnitCost.Should().Be(25);
            }

            var cancel = await Client.PostAsync(
                $"/api/customer-returns/{draft.Id}/cancel",
                null);
            cancel.StatusCode.Should().Be(HttpStatusCode.OK);
            var cancelled = await cancel.Content
                .ReadFromJsonAsync<CustomerReturnResponse>();
            cancelled!.Status.Should().Be(CustomerReturnStatus.Cancelled);
            var repeatedCancel = await Client.PostAsync(
                $"/api/customer-returns/{draft.Id}/cancel",
                null);
            repeatedCancel.StatusCode.Should().Be(HttpStatusCode.OK);

            using var finalScope = _factory.Services.CreateScope();
            var finalDb = finalScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await finalDb.Products.SingleAsync(x => x.Id == seed.ProductId))
                .Quantity.Should().Be(7);
            (await finalDb.Customers.SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(141.60m);
            (await finalDb.StockMovements.SingleAsync(x =>
                x.SourceType == "CustomerReturnCancellation" &&
                x.SourceId == draft.Id.ToString()))
                .QuantityChange.Should().Be(-2);
            (await finalDb.StockMovements.CountAsync(x =>
                x.SourceId == draft.Id.ToString() &&
                (x.SourceType == "CustomerReturn" ||
                 x.SourceType == "CustomerReturnCancellation")))
                .Should().Be(2);
        }

        [Fact]
        public async Task Post_Should_Reject_Quantity_Already_Returned()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var invoice = await CreateAndPostInvoiceAsync(seed);
            var first = await CreateReturnAsync(invoice, 2);
            var second = await CreateReturnAsync(invoice, 2);

            (await Client.PostAsync(
                $"/api/customer-returns/{first.Id}/post",
                null)).EnsureSuccessStatusCode();
            var rejected = await Client.PostAsync(
                $"/api/customer-returns/{second.Id}/post",
                null);

            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await rejected.Content.ReadAsStringAsync();
            body.Should().Contain("remaining returnable quantity");
            body.Should().Contain("traceId");
        }

        [Fact]
        public async Task Create_Should_Reject_NonPosted_Invoice_And_Wrong_Item()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var invoice = await CreateInvoiceAsync(seed);

            var response = await Client.PostAsJsonAsync(
                "/api/customer-returns",
                new Command
                {
                    ReturnNumber = $"RET-{Guid.NewGuid():N}",
                    SalesInvoiceId = invoice.Id,
                    ReturnDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new CustomerReturnItemInput
                        {
                            SalesInvoiceItemId = invoice.Items[0].Id + 99999,
                            Quantity = 1
                        }
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync())
                .Should().Contain("posted sales invoices");
        }

        [Fact]
        public async Task Post_Paid_Invoice_Return_Should_Create_Customer_Credit()
        {
            await AuthenticateAsync();
            var seed = await SeedAsync();
            var invoice = await CreateAndPostInvoiceAsync(seed);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var persisted = await db.SalesInvoices
                    .Include(x => x.Customer)
                    .SingleAsync(x => x.Id == invoice.Id);
                persisted.Status = SalesInvoiceStatus.Paid;
                persisted.AmountPaid = persisted.GrandTotal;
                persisted.BalanceDue = 0;
                persisted.Customer.BalanceDue = 0;
                await db.SaveChangesAsync();
            }

            var customerReturn = await CreateReturnAsync(invoice, 1);
            (await Client.PostAsync(
                $"/api/customer-returns/{customerReturn.Id}/post",
                null)).EnsureSuccessStatusCode();

            using var finalScope = _factory.Services.CreateScope();
            var finalDb = finalScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await finalDb.Customers.SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(-47.20m);
        }

        private async Task<CustomerReturnResponse> CreateReturnAsync(
            SalesInvoiceResponse invoice,
            decimal quantity)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/customer-returns",
                new Command
                {
                    ReturnNumber = $"RET-{Guid.NewGuid():N}",
                    SalesInvoiceId = invoice.Id,
                    ReturnDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new CustomerReturnItemInput
                        {
                            SalesInvoiceItemId = invoice.Items[0].Id,
                            Quantity = quantity
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<CustomerReturnResponse>())!;
        }

        private async Task<SalesInvoiceResponse> CreateAndPostInvoiceAsync(
            SeedResult seed)
        {
            var invoice = await CreateInvoiceAsync(seed);
            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/post",
                null);
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private async Task<SalesInvoiceResponse> CreateInvoiceAsync(
            SeedResult seed)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateInvoiceCommand
                {
                    InvoiceNumber = $"INV-{Guid.NewGuid():N}",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Items =
                    {
                        new CreateInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 3,
                            SellingUnitPrice = 40,
                            TaxRate = 18
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private async Task<SeedResult> SeedAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Return customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Return product {suffix}",
                SKU = $"RET-{suffix}",
                Quantity = 10,
                AverageCost = 25,
                Category = new Category
                {
                    Name = $"Return category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.AddRange(customer, product);
            await db.SaveChangesAsync();
            return new SeedResult(customer.Id, product.Id);
        }

        private sealed record SeedResult(int CustomerId, int ProductId);
    }
}
