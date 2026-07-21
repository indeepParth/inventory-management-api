using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.DeliveryChallans;
using InventoryManagement.Application.Features.Payments;
using InventoryManagement.Application.Features.Purchases;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using CreateChallanCommand =
    InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan.Command;
using CreateChallanItemInput =
    InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan.DeliveryChallanItemInput;
using CreateChallanInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans.Command;
using CreateChallanInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans.ChallanItemInput;
using CreateDirectInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.Command;
using CreateDirectInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.SalesInvoiceItemInput;
using CreatePaymentCommand =
    InventoryManagement.Application.Features.Payments.CreatePayment.Command;
using CreatePurchaseCommand =
    InventoryManagement.Application.Features.Purchases.CreatePurchase.Command;
using CreatePurchaseItemInput =
    InventoryManagement.Application.Features.Purchases.CreatePurchase.PurchaseItemInput;
using ReversePaymentCommand =
    InventoryManagement.Application.Features.Payments.ReversePayment.Command;
using UpdateDirectInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.Command;
using UpdateDirectInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.SalesInvoiceItemInput;

namespace InventoryManagement.Tests.IntegrationTests.DocumentNumbers
{
    public class AutomaticDocumentNumberingTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public AutomaticDocumentNumberingTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_Flows_Should_Generate_Calendar_Year_Document_Numbers()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();

            var firstPurchase = await CreatePurchaseAsync(
                seed,
                new DateTime(2026, 7, 1));
            var secondPurchase = await CreatePurchaseAsync(
                seed,
                new DateTime(2026, 7, 2));
            var nextYearPurchase = await CreatePurchaseAsync(
                seed,
                new DateTime(2027, 1, 1));

            firstPurchase.PurchaseNumber.Should().Be("P_2026_0001");
            secondPurchase.PurchaseNumber.Should().Be("P_2026_0002");
            nextYearPurchase.PurchaseNumber.Should().Be("P_2027_0001");

            var challan = await CreateAndPostChallanAsync(seed);
            challan.ChallanNumber.Should().Be("CH_2026_0001");

            var challanInvoice = await CreateChallanInvoiceAsync(
                challan.Items.Single().Id);
            challanInvoice.InvoiceNumber.Should().Be("IN_2026_0001");

            var directInvoice = await CreateDirectInvoiceAsync(seed);
            directInvoice.InvoiceNumber.Should().Be("IN_2026_0002_D");

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{directInvoice.Id}",
                new UpdateDirectInvoiceCommand(
                    0,
                    "MANUAL-CHANGE-SHOULD-BE-IGNORED",
                    seed.CustomerId,
                    new DateTime(2026, 7, 5),
                    0,
                    0,
                    "Updated",
                    new List<UpdateDirectInvoiceItemInput>
                    {
                        new()
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 30
                        }
                    }));
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedInvoice = await updateResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.InvoiceNumber.Should().Be("IN_2026_0002_D");

            (await Client.PostAsync(
                $"/api/sales-invoices/{directInvoice.Id}/post",
                null)).EnsureSuccessStatusCode();

            var paymentResponse = await Client.PostAsJsonAsync(
                "/api/payments",
                new CreatePaymentCommand
                {
                    CustomerId = seed.CustomerId,
                    SalesInvoiceId = directInvoice.Id,
                    PaymentDate = new DateTime(2026, 7, 6),
                    Amount = 10,
                    Method = PaymentMethod.Cash
                });
            paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
            payment.Should().NotBeNull();
            payment!.ReceiptNumber.Should().Be("R_2026_0001");

            var reversalResponse = await Client.PostAsJsonAsync(
                $"/api/payments/{payment.Id}/reverse",
                new ReversePaymentCommand
                {
                    PaymentDate = new DateTime(2026, 7, 7),
                    Note = "Reverse generated receipt"
                });
            reversalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var reversal = await reversalResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();
            reversal.Should().NotBeNull();
            reversal!.ReceiptNumber.Should().Be("R_2026_0002");
        }

        private async Task<SeedResult> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var supplier = new Supplier
            {
                Name = $"Number supplier {suffix}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var customer = new Customer
            {
                Name = $"Number customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Number product {suffix}",
                SKU = $"NUM-{suffix}",
                Quantity = 100,
                AverageCost = 10,
                Category = new Category
                {
                    Name = $"Number category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.AddRange(supplier, customer, product);
            await db.SaveChangesAsync();

            return new SeedResult(supplier.Id, customer.Id, product.Id);
        }

        private async Task<PurchaseResponse> CreatePurchaseAsync(
            SeedResult seed,
            DateTime billDate)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/purchases",
                new CreatePurchaseCommand
                {
                    PurchaseNumber = "MANUAL-PURCHASE-IGNORED",
                    SupplierId = seed.SupplierId,
                    BillDate = billDate,
                    Items =
                    {
                        new CreatePurchaseItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            UnitCost = 10
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PurchaseResponse>())!;
        }

        private async Task<DeliveryChallanResponse> CreateAndPostChallanAsync(
            SeedResult seed)
        {
            var createResponse = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                new CreateChallanCommand
                {
                    ChallanNumber = "MANUAL-CHALLAN-IGNORED",
                    CustomerId = seed.CustomerId,
                    ChallanDate = new DateTime(2026, 7, 3),
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site",
                    Items =
                    {
                        new CreateChallanItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1
                        }
                    }
                });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>())!;

            var postResponse = await Client.PostAsync(
                $"/api/delivery-challans/{created.Id}/post",
                null);
            postResponse.EnsureSuccessStatusCode();
            return (await postResponse.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>())!;
        }

        private async Task<SalesInvoiceResponse> CreateChallanInvoiceAsync(
            int challanItemId)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new CreateChallanInvoiceCommand
                {
                    InvoiceNumber = "MANUAL-CHALLAN-INVOICE-IGNORED",
                    InvoiceDate = new DateTime(2026, 7, 4),
                    Items =
                    {
                        new CreateChallanInvoiceItemInput
                        {
                            DeliveryChallanItemId = challanItemId,
                            SellingUnitPrice = 20
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private async Task<SalesInvoiceResponse> CreateDirectInvoiceAsync(
            SeedResult seed)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateDirectInvoiceCommand
                {
                    InvoiceNumber = "MANUAL-DIRECT-INVOICE-IGNORED",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 4),
                    Items =
                    {
                        new CreateDirectInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 30
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private sealed record SeedResult(
            int SupplierId,
            int CustomerId,
            int ProductId);
    }
}
