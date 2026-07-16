using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallanById;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.GetDeliveryChallanById
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Map_Linked_Driver_Name_And_Charge_Fields()
        {
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryChallan
                {
                    Id = 1,
                    ChallanNumber = "DC-1",
                    CustomerId = 2,
                    Customer = new Customer { Id = 2, Name = "Customer" },
                    Status = DeliveryChallanStatus.Draft,
                    DriverId = 3,
                    Driver = new Driver { Id = 3, Name = "Linked driver" },
                    DriverName = "Legacy driver",
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site",
                    DeliveryCharge = 125,
                    IsDeliveryChargePaid = true
                });

            var result = await new Handler(repository.Object)
                .Handle(new Query { Id = 1 }, CancellationToken.None);

            result.DriverId.Should().Be(3);
            result.DriverName.Should().Be("Linked driver");
            result.DeliveryFromAddress.Should().Be("Warehouse");
            result.DeliveryCharge.Should().Be(125);
            result.IsDeliveryChargePaid.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Map_Legacy_Driver_Name_When_No_Linked_Driver()
        {
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryChallan
                {
                    Id = 1,
                    ChallanNumber = "DC-1",
                    CustomerId = 2,
                    Customer = new Customer { Id = 2, Name = "Customer" },
                    Status = DeliveryChallanStatus.Draft,
                    DriverName = "Legacy driver",
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site"
                });

            var result = await new Handler(repository.Object)
                .Handle(new Query { Id = 1 }, CancellationToken.None);

            result.DriverId.Should().BeNull();
            result.DriverName.Should().Be("Legacy driver");
        }
    }
}
