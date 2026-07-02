using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Statements;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Statements
{
    public class StatementEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public StatementEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Customer_Statement_Should_Calculate_Opening_Running_And_Summary_Balances()
        {
            await AuthenticateAsync();
            var customerId = await SeedCustomerStatementAsync();

            var firstResponse = await Client.GetAsync(
                $"/api/customers/{customerId}/statement" +
                "?dateFrom=2026-03-01&dateTo=2026-03-31&pageNumber=1&pageSize=2");

            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var first = await firstResponse.Content
                .ReadFromJsonAsync<StatementResponse>();
            first.Should().NotBeNull();
            first!.OpeningBalance.Should().Be(80);
            first.ClosingBalance.Should().Be(130);
            first.TotalCharges.Should().Be(50);
            first.TotalPayments.Should().Be(30);
            first.TotalReversals.Should().Be(30);
            first.TotalCount.Should().Be(3);
            first.TotalPages.Should().Be(2);
            first.Entries.Select(x => x.Type).Should()
                .ContainInOrder(StatementEntryType.Invoice, StatementEntryType.Payment);
            first.Entries.Select(x => x.RunningBalance).Should()
                .ContainInOrder(130, 100);

            var second = await Client.GetFromJsonAsync<StatementResponse>(
                $"/api/customers/{customerId}/statement" +
                "?dateFrom=2026-03-01&dateTo=2026-03-31&pageNumber=2&pageSize=2");
            second.Should().NotBeNull();
            second!.Entries.Should().ContainSingle();
            second.Entries[0].Type.Should().Be(StatementEntryType.Reversal);
            second.Entries[0].RunningBalance.Should().Be(130);
        }

        [Fact]
        public async Task Supplier_Statement_Should_Calculate_Equivalent_Purchase_Ledger()
        {
            await AuthenticateAsync();
            var supplierId = await SeedSupplierStatementAsync();

            var response = await Client.GetAsync(
                $"/api/suppliers/{supplierId}/statement" +
                "?dateFrom=2026-03-01&dateTo=2026-03-31&pageNumber=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var statement = await response.Content
                .ReadFromJsonAsync<StatementResponse>();
            statement.Should().NotBeNull();
            statement!.OpeningBalance.Should().Be(150);
            statement.ClosingBalance.Should().Be(250);
            statement.TotalCharges.Should().Be(100);
            statement.TotalPayments.Should().Be(40);
            statement.TotalReversals.Should().Be(40);
            statement.Entries.Select(x => x.Type).Should().ContainInOrder(
                StatementEntryType.Purchase,
                StatementEntryType.Payment,
                StatementEntryType.Reversal);
            statement.Entries.Select(x => x.RunningBalance).Should()
                .ContainInOrder(250, 210, 250);
        }

        [Fact]
        public async Task Statement_Should_Return_Structured_Validation_For_Invalid_Range()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync(
                "/api/customers/1/statement" +
                "?dateFrom=2026-04-01&dateTo=2026-03-01");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("traceId");
        }

        private async Task<int> SeedCustomerStatementAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Statement customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var openingInvoice = CustomerInvoice(
                customer, $"OPEN-{suffix}", new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), 100);
            var periodInvoice = CustomerInvoice(
                customer, $"PERIOD-{suffix}", new DateTime(2026, 3, 2),
                new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc), 50);
            var ignoredDraft = CustomerInvoice(
                customer, $"DRAFT-{suffix}", new DateTime(2026, 3, 3),
                new DateTime(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc), 999);
            ignoredDraft.Status = SalesInvoiceStatus.Draft;
            db.SalesInvoices.AddRange(openingInvoice, periodInvoice, ignoredDraft);
            db.Payments.AddRange(
                CustomerPayment(customer, $"OPEN-PAY-{suffix}",
                    new DateTime(2026, 1, 5), new DateTime(2026, 1, 5, 9, 0, 0,
                        DateTimeKind.Utc), 20),
                CustomerPayment(customer, $"PAY-{suffix}",
                    new DateTime(2026, 3, 2), new DateTime(2026, 3, 2, 9, 0, 0,
                        DateTimeKind.Utc), 30));
            await db.SaveChangesAsync();

            var original = db.Payments.Local.Single(x =>
                x.ReceiptNumber == $"PAY-{suffix}");
            db.Payments.Add(CustomerPayment(
                customer, $"REV-{suffix}", new DateTime(2026, 3, 2),
                new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc), -30,
                original.Id));
            await db.SaveChangesAsync();
            return customer.Id;
        }

        private async Task<int> SeedSupplierStatementAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var supplier = new Supplier
            {
                Name = $"Statement supplier {suffix}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Purchases.AddRange(
                SupplierPurchase(supplier, $"OPEN-{suffix}",
                    new DateTime(2026, 1, 1), new DateTime(2026, 1, 1, 8, 0, 0,
                        DateTimeKind.Utc), 200),
                SupplierPurchase(supplier, $"PERIOD-{suffix}",
                    new DateTime(2026, 3, 2), new DateTime(2026, 3, 2, 8, 0, 0,
                        DateTimeKind.Utc), 100));
            db.Payments.AddRange(
                SupplierPayment(supplier, $"OPEN-PAY-{suffix}",
                    new DateTime(2026, 1, 5), new DateTime(2026, 1, 5, 9, 0, 0,
                        DateTimeKind.Utc), 50),
                SupplierPayment(supplier, $"PAY-{suffix}",
                    new DateTime(2026, 3, 2), new DateTime(2026, 3, 2, 9, 0, 0,
                        DateTimeKind.Utc), 40));
            await db.SaveChangesAsync();

            var original = db.Payments.Local.Single(x =>
                x.ReceiptNumber == $"PAY-{suffix}");
            db.Payments.Add(SupplierPayment(
                supplier, $"REV-{suffix}", new DateTime(2026, 3, 2),
                new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc), -40,
                original.Id));
            await db.SaveChangesAsync();
            return supplier.Id;
        }

        private static SalesInvoice CustomerInvoice(
            Customer customer, string number, DateTime date, DateTime timestamp,
            decimal amount) => new()
        {
            InvoiceNumber = number,
            Customer = customer,
            InvoiceDate = date,
            Status = SalesInvoiceStatus.Posted,
            GrandTotal = amount,
            BalanceDue = amount,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            PostedAtUtc = timestamp,
            CreatedBy = "test"
        };

        private static Purchase SupplierPurchase(
            Supplier supplier, string number, DateTime date, DateTime timestamp,
            decimal amount) => new()
        {
            PurchaseNumber = number,
            Supplier = supplier,
            BillDate = date,
            Status = PurchaseStatus.Posted,
            GrandTotal = amount,
            BalanceDue = amount,
            CreatedAtUtc = timestamp,
            PostedAtUtc = timestamp,
            CreatedBy = "test"
        };

        private static Payment CustomerPayment(
            Customer customer, string number, DateTime date, DateTime timestamp,
            decimal amount, int? reversesPaymentId = null) => new()
        {
            ReceiptNumber = number,
            Customer = customer,
            PaymentDate = date,
            Amount = amount,
            Method = PaymentMethod.Cash,
            CreatedAtUtc = timestamp,
            CreatedBy = "test",
            ReversesPaymentId = reversesPaymentId
        };

        private static Payment SupplierPayment(
            Supplier supplier, string number, DateTime date, DateTime timestamp,
            decimal amount, int? reversesPaymentId = null) => new()
        {
            ReceiptNumber = number,
            Supplier = supplier,
            PaymentDate = date,
            Amount = amount,
            Method = PaymentMethod.BankTransfer,
            CreatedAtUtc = timestamp,
            CreatedBy = "test",
            ReversesPaymentId = reversesPaymentId
        };
    }
}
