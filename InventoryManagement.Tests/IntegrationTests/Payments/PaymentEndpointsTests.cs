using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Features.Payments;
using InventoryManagement.Application.Features.Payments.CreatePayment;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Payments
{
    public class PaymentEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public PaymentEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_Should_Atomically_Update_Invoice_And_Customer()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedInvoiceAsync(100);

            var response = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = $"RCPT-{Guid.NewGuid():N}",
                CustomerId = seed.CustomerId,
                SalesInvoiceId = seed.InvoiceId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 40,
                Method = PaymentMethod.UPI,
                ExternalReference = " UPI-123 ",
                Note = " First payment "
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            payment.Should().NotBeNull();
            payment!.Amount.Should().Be(40);
            payment.ExternalReference.Should().Be("UPI-123");
            payment.Note.Should().Be("First payment");
            payment.CreatedBy.Should().NotBeNullOrWhiteSpace();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = await db.SalesInvoices.AsNoTracking()
                .SingleAsync(x => x.Id == seed.InvoiceId);
            invoice.AmountPaid.Should().Be(40);
            invoice.BalanceDue.Should().Be(60);
            invoice.Status.Should().Be(SalesInvoiceStatus.PartiallyPaid);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(60);
        }

        [Fact]
        public async Task Create_Overpayment_Should_Roll_Back_All_Changes()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedInvoiceAsync(100);
            var receiptNumber = $"OVER-{Guid.NewGuid():N}";

            var response = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = receiptNumber,
                CustomerId = seed.CustomerId,
                SalesInvoiceId = seed.InvoiceId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 100.01m,
                Method = PaymentMethod.Cash
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Payments.CountAsync(x => x.ReceiptNumber == receiptNumber))
                .Should().Be(0);
            var invoice = await db.SalesInvoices.AsNoTracking()
                .SingleAsync(x => x.Id == seed.InvoiceId);
            invoice.AmountPaid.Should().Be(0);
            invoice.BalanceDue.Should().Be(100);
            invoice.Status.Should().Be(SalesInvoiceStatus.Posted);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(100);
        }

        [Fact]
        public async Task Reverse_Should_Create_Compensating_Record_And_Restore_Balances()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedInvoiceAsync(100);
            var createResponse = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = $"RCPT-{Guid.NewGuid():N}",
                CustomerId = seed.CustomerId,
                SalesInvoiceId = seed.InvoiceId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 100,
                Method = PaymentMethod.BankTransfer
            });
            var original = (await createResponse.Content
                .ReadFromJsonAsync<PaymentResponse>())!;

            var response = await Client.PostAsJsonAsync(
                $"/api/payments/{original.Id}/reverse",
                new InventoryManagement.Application.Features.Payments.ReversePayment.Command
                {
                    ReceiptNumber = $"REV-{Guid.NewGuid():N}",
                    PaymentDate = new DateTime(2026, 7, 3),
                    Note = "Bank return"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var reversal = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            reversal.Should().NotBeNull();
            reversal!.Amount.Should().Be(-100);
            reversal.ReversesPaymentId.Should().Be(original.Id);
            reversal.Method.Should().Be(PaymentMethod.BankTransfer);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                (await db.Payments.AsNoTracking().CountAsync(x =>
                    x.Id == original.Id || x.ReversesPaymentId == original.Id))
                    .Should().Be(2);
                var invoice = await db.SalesInvoices.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.InvoiceId);
                invoice.AmountPaid.Should().Be(0);
                invoice.BalanceDue.Should().Be(100);
                invoice.Status.Should().Be(SalesInvoiceStatus.Posted);
                (await db.Customers.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue.Should().Be(100);
            }

            (await Client.PostAsJsonAsync(
                $"/api/payments/{original.Id}/reverse",
                new InventoryManagement.Application.Features.Payments.ReversePayment.Command
                {
                    ReceiptNumber = $"REV2-{Guid.NewGuid():N}",
                    PaymentDate = new DateTime(2026, 7, 4)
                })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Get_Should_Return_Filtered_Paginated_Immutable_Ledger()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedInvoiceAsync(100);
            var receipt = $"FILTER-{Guid.NewGuid():N}";
            (await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = receipt,
                CustomerId = seed.CustomerId,
                SalesInvoiceId = seed.InvoiceId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 25,
                Method = PaymentMethod.Cheque
            })).EnsureSuccessStatusCode();

            var response = await Client.GetAsync(
                $"/api/payments?pageNumber=1&pageSize=1&customerId={seed.CustomerId}" +
                $"&salesInvoiceId={seed.InvoiceId}&method=Cheque" +
                $"&dateFrom=2026-07-01&dateTo=2026-07-31&receiptNumber={receipt[..12]}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content
                .ReadFromJsonAsync<PagedResponse<PaymentResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x => x.ReceiptNumber == receipt);
            page.TotalCount.Should().Be(1);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task Supplier_Payments_Should_Progress_Purchase_To_Paid_And_Filter()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedPurchaseAsync(100);
            var firstNumber = $"SUP-PAY-{Guid.NewGuid():N}";

            var firstResponse = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = firstNumber,
                SupplierId = seed.SupplierId,
                PurchaseId = seed.PurchaseId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 40,
                Method = PaymentMethod.BankTransfer,
                ExternalReference = " BANK-42 "
            });

            firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var first = await firstResponse.Content.ReadFromJsonAsync<PaymentResponse>();
            first.Should().NotBeNull();
            first!.CustomerId.Should().BeNull();
            first.SupplierId.Should().Be(seed.SupplierId);
            first.PurchaseId.Should().Be(seed.PurchaseId);
            first.Amount.Should().Be(40);
            first.ExternalReference.Should().Be("BANK-42");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var purchase = await db.Purchases.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.PurchaseId);
                purchase.AmountPaid.Should().Be(40);
                purchase.BalanceDue.Should().Be(60);
                purchase.Status.Should().Be(PurchaseStatus.PartiallyPaid);
            }

            (await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = $"SUP-PAY-{Guid.NewGuid():N}",
                SupplierId = seed.SupplierId,
                PurchaseId = seed.PurchaseId,
                PaymentDate = new DateTime(2026, 7, 3),
                Amount = 60,
                Method = PaymentMethod.Cheque
            })).EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var purchase = await db.Purchases.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.PurchaseId);
                purchase.AmountPaid.Should().Be(100);
                purchase.BalanceDue.Should().Be(0);
                purchase.Status.Should().Be(PurchaseStatus.Paid);
            }

            var getResponse = await Client.GetAsync(
                $"/api/payments?pageNumber=1&pageSize=1" +
                $"&supplierId={seed.SupplierId}&purchaseId={seed.PurchaseId}" +
                $"&method=BankTransfer&receiptNumber={firstNumber[..14]}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await getResponse.Content
                .ReadFromJsonAsync<PagedResponse<PaymentResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x => x.Id == first.Id);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Supplier_Overpayment_Should_Roll_Back_All_Changes()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedPurchaseAsync(100);
            var paymentNumber = $"SUP-OVER-{Guid.NewGuid():N}";

            var response = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = paymentNumber,
                SupplierId = seed.SupplierId,
                PurchaseId = seed.PurchaseId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 100.01m,
                Method = PaymentMethod.Cash
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Payments.CountAsync(x => x.ReceiptNumber == paymentNumber))
                .Should().Be(0);
            var purchase = await db.Purchases.AsNoTracking()
                .SingleAsync(x => x.Id == seed.PurchaseId);
            purchase.AmountPaid.Should().Be(0);
            purchase.BalanceDue.Should().Be(100);
            purchase.Status.Should().Be(PurchaseStatus.Posted);
        }

        [Fact]
        public async Task Supplier_Payment_Reversal_Should_Create_Compensating_Record()
        {
            await AuthenticateAsync();
            var seed = await SeedPostedPurchaseAsync(100);
            var createResponse = await Client.PostAsJsonAsync("/api/payments", new Command
            {
                ReceiptNumber = $"SUP-ORIG-{Guid.NewGuid():N}",
                SupplierId = seed.SupplierId,
                PurchaseId = seed.PurchaseId,
                PaymentDate = new DateTime(2026, 7, 2),
                Amount = 100,
                Method = PaymentMethod.UPI
            });
            var original = (await createResponse.Content
                .ReadFromJsonAsync<PaymentResponse>())!;

            var reverseResponse = await Client.PostAsJsonAsync(
                $"/api/payments/{original.Id}/reverse",
                new InventoryManagement.Application.Features.Payments.ReversePayment.Command
                {
                    ReceiptNumber = $"SUP-REV-{Guid.NewGuid():N}",
                    PaymentDate = new DateTime(2026, 7, 3),
                    Note = "Returned by supplier"
                });

            reverseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var reversal = await reverseResponse.Content
                .ReadFromJsonAsync<PaymentResponse>();
            reversal.Should().NotBeNull();
            reversal!.Amount.Should().Be(-100);
            reversal.SupplierId.Should().Be(seed.SupplierId);
            reversal.PurchaseId.Should().Be(seed.PurchaseId);
            reversal.ReversesPaymentId.Should().Be(original.Id);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var purchase = await db.Purchases.AsNoTracking()
                .SingleAsync(x => x.Id == seed.PurchaseId);
            purchase.AmountPaid.Should().Be(0);
            purchase.BalanceDue.Should().Be(100);
            purchase.Status.Should().Be(PurchaseStatus.Posted);
            (await db.Payments.CountAsync(x =>
                x.Id == original.Id || x.ReversesPaymentId == original.Id))
                .Should().Be(2);
        }

        private async Task<SeedResult> SeedPostedInvoiceAsync(decimal total)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Payment customer {suffix}",
                IsActive = true,
                BalanceDue = total,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var invoice = new SalesInvoice
            {
                InvoiceNumber = $"PAY-INV-{suffix}",
                Customer = customer,
                InvoiceDate = new DateTime(2026, 7, 1),
                Status = SalesInvoiceStatus.Posted,
                GrandTotal = total,
                BalanceDue = total,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                PostedAtUtc = DateTime.UtcNow,
                CreatedBy = "test"
            };
            db.SalesInvoices.Add(invoice);
            await db.SaveChangesAsync();
            return new SeedResult(customer.Id, invoice.Id);
        }

        private async Task<SupplierSeedResult> SeedPostedPurchaseAsync(decimal total)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var supplier = new Supplier
            {
                Name = $"Payment supplier {suffix}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var purchase = new Purchase
            {
                PurchaseNumber = $"PAY-PUR-{suffix}",
                Supplier = supplier,
                BillDate = new DateTime(2026, 7, 1),
                Status = PurchaseStatus.Posted,
                GrandTotal = total,
                BalanceDue = total,
                CreatedAtUtc = DateTime.UtcNow,
                PostedAtUtc = DateTime.UtcNow,
                CreatedBy = "test"
            };
            db.Purchases.Add(purchase);
            await db.SaveChangesAsync();
            return new SupplierSeedResult(supplier.Id, purchase.Id);
        }

        private sealed record SeedResult(int CustomerId, int InvoiceId);
        private sealed record SupplierSeedResult(int SupplierId, int PurchaseId);
    }
}
