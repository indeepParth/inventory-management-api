using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Drivers.GetDriverDeliveries;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Drivers.GetDriverDeliveries
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Pass_Date_Range_And_Return_Driver_Summary()
        {
            var driver = new Driver
            {
                Id = 1,
                Name = "Driver",
                Phone = "999",
                LicenseNumber = "LIC-1",
                IsActive = true
            };
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(driver);
            var challans = new Mock<IDeliveryChallanRepository>();
            var dateFrom = new DateTime(2026, 7, 1);
            var dateTo = new DateTime(2026, 7, 31);
            challans.Setup(x => x.GetDriverDeliveriesAsync(
                    1,
                    dateFrom,
                    dateTo,
                    null,
                    2,
                    5,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DeliveryChallan> { Challan(true) });
            challans.Setup(x => x.GetDriverDeliveriesCountAsync(
                    1,
                    dateFrom,
                    dateTo,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await new Handler(drivers.Object, challans.Object)
                .Handle(new Query
                {
                    DriverId = 1,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    PageNumber = 2,
                    PageSize = 5
                }, CancellationToken.None);

            result.Id.Should().Be(1);
            result.Name.Should().Be("Driver");
            result.Phone.Should().Be("999");
            result.LicenseNumber.Should().Be("LIC-1");
            result.Deliveries.PageNumber.Should().Be(2);
            result.Deliveries.PageSize.Should().Be(5);
            result.Deliveries.TotalCount.Should().Be(1);
            result.Deliveries.Items.Should().ContainSingle(x =>
                x.CustomerName == "Customer" &&
                x.DeliveryFromAddress == "Warehouse" &&
                x.DeliveryToAddress == "Customer site" &&
                x.ItemCount == 2);
        }

        [Theory]
        [InlineData(DriverDeliveryPaymentStatus.Paid, true)]
        [InlineData(DriverDeliveryPaymentStatus.Unpaid, false)]
        public async Task Handle_Should_Map_Payment_Status_To_Paid_Filter(
            DriverDeliveryPaymentStatus paymentStatus,
            bool expectedPaidFilter)
        {
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Driver { Id = 1, Name = "Driver" });
            var challans = new Mock<IDeliveryChallanRepository>();
            challans.Setup(x => x.GetDriverDeliveriesAsync(
                    1,
                    null,
                    null,
                    expectedPaidFilter,
                    1,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DeliveryChallan> { Challan(expectedPaidFilter) });
            challans.Setup(x => x.GetDriverDeliveriesCountAsync(
                    1,
                    null,
                    null,
                    expectedPaidFilter,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await new Handler(drivers.Object, challans.Object)
                .Handle(new Query
                {
                    DriverId = 1,
                    PaymentStatus = paymentStatus
                }, CancellationToken.None);

            result.Deliveries.Items.Should().ContainSingle(x =>
                x.IsDeliveryChargePaid == expectedPaidFilter);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Driver_Not_Found()
        {
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(404, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver?)null);
            var challans = new Mock<IDeliveryChallanRepository>();

            var action = () => new Handler(drivers.Object, challans.Object)
                .Handle(new Query { DriverId = 404 }, CancellationToken.None);

            await action.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Driver not found.");
            challans.Verify(x => x.GetDriverDeliveriesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        private static DeliveryChallan Challan(bool isPaid) => new()
        {
            Id = 10,
            ChallanNumber = "DC-1",
            ChallanDate = new DateTime(2026, 7, 10),
            Status = DeliveryChallanStatus.Posted,
            Customer = new Customer { Id = 2, Name = "Customer" },
            DeliveryFromAddress = "Warehouse",
            DeliveryAddress = "Customer site",
            VehicleNumber = "MH-01",
            DeliveryCharge = 100,
            IsDeliveryChargePaid = isPaid,
            Items =
            {
                new DeliveryChallanItem { Id = 1 },
                new DeliveryChallanItem { Id = 2 }
            }
        };
    }
}
