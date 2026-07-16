using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Features.Drivers.GetDriverDeliveries;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests.IntegrationTests.Drivers
{
    public class DriverDeliveriesEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public DriverDeliveriesEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetDeliveries_Should_Return_Only_Posted_And_Invoiced_By_Default()
        {
            await AuthenticateAsync();
            var seed = await SeedDriverDeliveriesAsync();

            var response = await Client.GetAsync($"/api/drivers/{seed.DriverId}/deliveries");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content
                .ReadFromJsonAsync<DriverDeliveriesResponse>();
            result.Should().NotBeNull();
            result!.Id.Should().Be(seed.DriverId);
            result.Name.Should().Be(seed.DriverName);
            result.Deliveries.TotalCount.Should().Be(2);
            result.Deliveries.Items.Should().OnlyContain(x =>
                x.Status == DeliveryChallanStatus.Posted ||
                x.Status == DeliveryChallanStatus.Invoiced);
            result.Deliveries.Items.Should().ContainSingle(x =>
                x.ChallanId == seed.PostedPaidChallanId &&
                x.CustomerName == seed.CustomerName &&
                x.DeliveryFromAddress == "Main warehouse" &&
                x.DeliveryToAddress == "Paid customer site" &&
                x.VehicleNumber == "TRUCK-1" &&
                x.DeliveryCharge == 100 &&
                x.IsDeliveryChargePaid &&
                x.ItemCount == 2);
        }

        [Fact]
        public async Task GetDeliveries_Should_Filter_By_Date_Range()
        {
            await AuthenticateAsync();
            var seed = await SeedDriverDeliveriesAsync();

            var response = await Client.GetAsync(
                $"/api/drivers/{seed.DriverId}/deliveries" +
                "?dateFrom=2026-07-10&dateTo=2026-07-31");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content
                .ReadFromJsonAsync<DriverDeliveriesResponse>();
            result.Should().NotBeNull();
            result!.Deliveries.Items.Should().ContainSingle(x =>
                x.ChallanId == seed.InvoicedUnpaidChallanId);
        }

        [Theory]
        [InlineData("paid", true)]
        [InlineData("unpaid", false)]
        public async Task GetDeliveries_Should_Filter_By_Payment_Status(
            string paymentStatus,
            bool isPaid)
        {
            await AuthenticateAsync();
            var seed = await SeedDriverDeliveriesAsync();

            var response = await Client.GetAsync(
                $"/api/drivers/{seed.DriverId}/deliveries?paymentStatus={paymentStatus}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content
                .ReadFromJsonAsync<DriverDeliveriesResponse>();
            result.Should().NotBeNull();
            result!.Deliveries.Items.Should().ContainSingle(x =>
                x.IsDeliveryChargePaid == isPaid);
        }

        private async Task<SeedResult> SeedDriverDeliveriesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var driver = new Driver
            {
                Name = $"History driver {suffix}",
                Phone = "9999999999",
                LicenseNumber = $"LIC-{suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var customer = new Customer
            {
                Name = $"History customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var category = new Category
            {
                Name = $"History category {suffix}",
                Description = "Test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var firstProduct = new Product
            {
                Name = $"History product A {suffix}",
                SKU = $"HISTA-{suffix}",
                Quantity = 10,
                AverageCost = 20,
                Category = category
            };
            var secondProduct = new Product
            {
                Name = $"History product B {suffix}",
                SKU = $"HISTB-{suffix}",
                Quantity = 10,
                AverageCost = 30,
                Category = category
            };

            var postedPaid = Challan(
                suffix,
                "PAID",
                driver,
                customer,
                new DateTime(2026, 7, 5),
                DeliveryChallanStatus.Posted,
                "Paid customer site",
                100,
                isPaid: true,
                firstProduct,
                secondProduct);
            var invoicedUnpaid = Challan(
                suffix,
                "UNPAID",
                driver,
                customer,
                new DateTime(2026, 7, 20),
                DeliveryChallanStatus.Invoiced,
                "Unpaid customer site",
                150,
                isPaid: false,
                firstProduct);
            var draft = Challan(
                suffix,
                "DRAFT",
                driver,
                customer,
                new DateTime(2026, 7, 25),
                DeliveryChallanStatus.Draft,
                "Draft customer site",
                200,
                isPaid: false,
                firstProduct);
            var cancelled = Challan(
                suffix,
                "CANCELLED",
                driver,
                customer,
                new DateTime(2026, 7, 26),
                DeliveryChallanStatus.Cancelled,
                "Cancelled customer site",
                250,
                isPaid: false,
                firstProduct);
            db.DeliveryChallans.AddRange(postedPaid, invoicedUnpaid, draft, cancelled);
            await db.SaveChangesAsync();

            return new SeedResult(
                driver.Id,
                driver.Name,
                customer.Name,
                postedPaid.Id,
                invoicedUnpaid.Id);
        }

        private static DeliveryChallan Challan(
            string suffix,
            string label,
            Driver driver,
            Customer customer,
            DateTime challanDate,
            DeliveryChallanStatus status,
            string deliveryAddress,
            decimal deliveryCharge,
            bool isPaid,
            params Product[] products)
        {
            var challan = new DeliveryChallan
            {
                ChallanNumber = $"DC-{label}-{suffix}",
                Driver = driver,
                Customer = customer,
                ChallanDate = challanDate,
                Status = status,
                VehicleNumber = "TRUCK-1",
                DeliveryFromAddress = "Main warehouse",
                DeliveryAddress = deliveryAddress,
                DeliveryCharge = deliveryCharge,
                IsDeliveryChargePaid = isPaid,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test"
            };

            foreach (var product in products)
            {
                challan.Items.Add(new DeliveryChallanItem
                {
                    Product = product,
                    Quantity = 1
                });
            }

            return challan;
        }

        private sealed record SeedResult(
            int DriverId,
            string DriverName,
            string CustomerName,
            int PostedPaidChallanId,
            int InvoicedUnpaidChallanId);
    }
}
