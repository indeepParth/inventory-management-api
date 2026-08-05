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
                    1,
                    int.MaxValue,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DeliveryChallan> { Challan(true) });
            var invoices = new Mock<ISalesInvoiceRepository>();
            invoices.Setup(x => x.GetDriverDeliveriesAsync(
                    1,
                    dateFrom,
                    dateTo,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SalesInvoice> { Invoice(false) });

            var result = await new Handler(drivers.Object, challans.Object, invoices.Object)
                .Handle(new Query
                {
                    DriverId = 1,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    PageNumber = 1,
                    PageSize = 5
                }, CancellationToken.None);

            result.Id.Should().Be(1);
            result.Name.Should().Be("Driver");
            result.Phone.Should().Be("999");
            result.LicenseNumber.Should().Be("LIC-1");
            result.Deliveries.PageNumber.Should().Be(1);
            result.Deliveries.PageSize.Should().Be(5);
            result.Deliveries.TotalCount.Should().Be(2);
            result.Deliveries.Items.Should().Contain(x =>
                x.SourceType == DriverDeliverySourceType.Challan &&
                x.CustomerName == "Customer" &&
                x.DeliveryFromAddress == "Warehouse" &&
                x.DeliveryToAddress == "Customer site" &&
                x.LaborCharge == 0 &&
                x.ItemCount == 2);
            result.Deliveries.Items.Should().Contain(x =>
                x.SourceType == DriverDeliverySourceType.Invoice &&
                x.DocumentNumber == "IN-1" &&
                x.DeliveryToAddress == "Invoice site" &&
                x.LaborCharge == 35 &&
                !x.IsDeliveryChargePaid);
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
                    int.MaxValue,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DeliveryChallan> { Challan(expectedPaidFilter) });
            var invoices = new Mock<ISalesInvoiceRepository>();
            invoices.Setup(x => x.GetDriverDeliveriesAsync(
                    1,
                    null,
                    null,
                    expectedPaidFilter,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SalesInvoice>());

            var result = await new Handler(drivers.Object, challans.Object, invoices.Object)
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
            var invoices = new Mock<ISalesInvoiceRepository>();

            var action = () => new Handler(drivers.Object, challans.Object, invoices.Object)
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
            invoices.Verify(x => x.GetDriverDeliveriesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool?>(),
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

        private static SalesInvoice Invoice(bool isPaid) => new()
        {
            Id = 20,
            InvoiceNumber = "IN-1",
            InvoiceDate = new DateTime(2026, 7, 11),
            Status = SalesInvoiceStatus.Posted,
            Customer = new Customer { Id = 2, Name = "Customer" },
            DeliveryAddress = "Invoice site",
            OtherCharges = 75,
            LaborCharge = 35,
            IsDeliveryChargePaid = isPaid,
            Items =
            {
                new SalesInvoiceItem { Id = 1 }
            }
        };
    }
}
