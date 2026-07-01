using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdateSalesInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.Command;
using UpdateSalesInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.SalesInvoiceItemInput;

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

        [Fact]
        public async Task List_Should_Page_And_Apply_All_Filters()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var matchingNumber = $"MATCH-{Guid.NewGuid():N}";
            await CreateInvoiceAsync(
                seed,
                matchingNumber,
                new DateTime(2026, 7, 10));
            await CreateInvoiceAsync(
                seed,
                $"OTHER-{Guid.NewGuid():N}",
                new DateTime(2026, 6, 1));

            var response = await Client.GetAsync(
                $"/api/sales-invoices?pageNumber=1&pageSize=1" +
                $"&customerId={seed.CustomerId}&status=Draft" +
                "&dateFrom=2026-07-01&dateTo=2026-07-31" +
                $"&invoiceNumber={matchingNumber[..12]}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content
                .ReadFromJsonAsync<PagedResponse<SalesInvoiceResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x =>
                x.InvoiceNumber == matchingNumber);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Update_Draft_Should_Recalculate_Totals_And_Reject_Paid_Invoice()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var created = await CreateInvoiceAsync(
                seed,
                $"EDIT-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));
            var update = new UpdateSalesInvoiceCommand(
                0,
                $"EDITED-{Guid.NewGuid():N}",
                seed.CustomerId,
                new DateTime(2026, 7, 2),
                4,
                2,
                " Edited draft ",
                new List<UpdateSalesInvoiceItemInput>
                {
                    new()
                    {
                        ProductId = seed.ProductId,
                        Quantity = 3,
                        SellingUnitPrice = 20,
                        TaxRate = 5
                    }
                });

            var response = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{created.Id}",
                update);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(SalesInvoiceStatus.Draft);
            updated.Subtotal.Should().Be(60);
            updated.TaxAmount.Should().Be(3);
            updated.GrandTotal.Should().Be(61);
            updated.BalanceDue.Should().Be(61);
            updated.AmountPaid.Should().Be(0);
            updated.Notes.Should().Be("Edited draft");
            updated.Items.Should().ContainSingle();
            updated.Items[0].CostAtSale.Should().BeNull();
            updated.CreatedAtUtc.Should().Be(created.CreatedAtUtc);
            updated.CreatedBy.Should().Be(created.CreatedBy);
            updated.UpdatedAtUtc.Should().BeAfter(created.UpdatedAtUtc);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var invoice = await db.SalesInvoices
                    .SingleAsync(x => x.Id == created.Id);
                invoice.Status = SalesInvoiceStatus.Paid;
                await db.SaveChangesAsync();
            }

            var rejected = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{created.Id}",
                update);
            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        private async Task<SalesInvoiceResponse> CreateInvoiceAsync(
            SeedResult seed,
            string invoiceNumber,
            DateTime invoiceDate)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = seed.CustomerId,
                    InvoiceDate = invoiceDate,
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 10
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private sealed record SeedResult(
            int CustomerId,
            int ProductId,
            int DeliveryChallanItemId,
            decimal StockQuantity,
            int StockMovementCount);
    }
}
