using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.SalesInvoices
{
    public class SalesInvoiceEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public SalesInvoiceEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_Then_Get_Should_Return_Draft_Without_Changing_Stock()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();

            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = $"INV-{Guid.NewGuid():N}",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Discount = 5,
                    OtherCharges = 2,
                    Notes = " Draft invoice ",
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 2.5m,
                            SellingUnitPrice = 40,
                            TaxRate = 18,
                            DeliveryChallanItemId = seed.DeliveryChallanItemId
                        }
                    }
                });

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            created.Should().NotBeNull();
            created!.Status.Should().Be(SalesInvoiceStatus.Draft);
            created.Subtotal.Should().Be(100);
            created.TaxAmount.Should().Be(18);
            created.GrandTotal.Should().Be(115);
            created.AmountPaid.Should().Be(0);
            created.BalanceDue.Should().Be(115);
            created.Notes.Should().Be("Draft invoice");
            created.Items.Should().ContainSingle();
            created.Items[0].LineTotal.Should().Be(118);
            created.Items[0].CostAtSale.Should().BeNull();
            created.Items[0].DeliveryChallanItemId
                .Should().Be(seed.DeliveryChallanItemId);

            var getResponse = await Client.GetAsync(
                $"/api/sales-invoices/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            fetched.Should().BeEquivalentTo(created);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId);
            product.Quantity.Should().Be(seed.StockQuantity);
            (await db.StockMovements.CountAsync(x => x.ProductId == seed.ProductId))
                .Should().Be(seed.StockMovementCount);
        }

        [Fact]
        public async Task Create_Should_Return_Structured_Validation_For_Empty_Items()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = " ",
                    CustomerId = 0,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Items = new List<SalesInvoiceItemInput>()
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("traceId");
        }

        private async Task<SeedResult> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Invoice customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Invoice product {suffix}",
                SKU = $"INV-{suffix}",
                Quantity = 12.5m,
                AverageCost = 25,
                Category = new Category
                {
                    Name = $"Invoice category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            var challan = new DeliveryChallan
            {
                ChallanNumber = $"DC-{suffix}",
                Customer = customer,
                ChallanDate = new DateTime(2026, 7, 1),
                Status = DeliveryChallanStatus.Posted,
                DeliveryAddress = "Test address",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test",
                Items =
                {
                    new DeliveryChallanItem
                    {
                        Product = product,
                        Quantity = 2.5m
                    }
                }
            };
            db.DeliveryChallans.Add(challan);
            await db.SaveChangesAsync();

            return new SeedResult(
                customer.Id,
                product.Id,
                challan.Items.Single().Id,
                product.Quantity,
                await db.StockMovements.CountAsync(x => x.ProductId == product.Id));
        }

        private sealed record SeedResult(
            int CustomerId,
            int ProductId,
            int DeliveryChallanItemId,
            decimal StockQuantity,
            int StockMovementCount);
    }
}
