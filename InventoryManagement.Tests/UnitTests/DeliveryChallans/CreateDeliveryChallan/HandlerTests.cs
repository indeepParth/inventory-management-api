using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.CreateDeliveryChallan
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Create_Draft_Without_Changing_Product()
        {
            DeliveryChallan? added = null;
            var challans = new Mock<IDeliveryChallanRepository>();
            challans.Setup(x => x.AddAsync(
                    It.IsAny<DeliveryChallan>(), It.IsAny<CancellationToken>()))
                .Callback<DeliveryChallan, CancellationToken>((x, _) => added = x)
                .Returns(Task.CompletedTask);
            challans.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Customer", IsActive = true });
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Driver { Id = 3, Name = "Driver", IsActive = true });
            var product = new Product
            {
                Id = 2, Name = "Product", SKU = "SKU-1", Quantity = 25
            };
            var products = new Mock<IProductRepository>();
            products.Setup(x => x.GetProductByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.Username).Returns("dispatcher");

            var response = await new Handler(
                challans.Object,
                customers.Object,
                drivers.Object,
                products.Object,
                currentUser.Object)
                .Handle(new Command
                {
                    ChallanNumber = " DC-1 ",
                    CustomerId = 1,
                    ChallanDate = new DateTime(2026, 7, 1),
                    DriverId = 3,
                    DriverName = "Legacy driver",
                    DeliveryFromAddress = " Main warehouse ",
                    DeliveryAddress = " Customer site ",
                    DeliveryCharge = 75,
                    Items = { new DeliveryChallanItemInput { ProductId = 2, Quantity = 1.5m } }
                }, CancellationToken.None);

            added.Should().NotBeNull();
            added!.Status.Should().Be(DeliveryChallanStatus.Draft);
            added.ChallanNumber.Should().Be("DC-1");
            added.DriverId.Should().Be(3);
            added.DriverName.Should().Be("Legacy driver");
            added.DeliveryFromAddress.Should().Be("Main warehouse");
            added.DeliveryAddress.Should().Be("Customer site");
            added.DeliveryCharge.Should().Be(75);
            added.IsDeliveryChargePaid.Should().BeFalse();
            added.CreatedBy.Should().Be("dispatcher");
            response.DriverId.Should().Be(3);
            response.DriverName.Should().Be("Driver");
            response.DeliveryFromAddress.Should().Be("Main warehouse");
            response.DeliveryCharge.Should().Be(75);
            response.IsDeliveryChargePaid.Should().BeFalse();
            response.Items.Should().ContainSingle(x => x.Quantity == 1.5m);
            product.Quantity.Should().Be(25);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Driver()
        {
            var challans = new Mock<IDeliveryChallanRepository>();
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Customer", IsActive = true });
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Driver { Id = 3, Name = "Driver", IsActive = false });

            var action = () => new Handler(
                challans.Object,
                customers.Object,
                drivers.Object,
                Mock.Of<IProductRepository>(),
                Mock.Of<ICurrentUserService>())
                .Handle(new Command
                {
                    ChallanNumber = "DC-1",
                    CustomerId = 1,
                    ChallanDate = new DateTime(2026, 7, 1),
                    DriverId = 3,
                    DeliveryFromAddress = "Warehouse",
                    DeliveryAddress = "Customer site",
                    Items = { new DeliveryChallanItemInput { ProductId = 2, Quantity = 1 } }
                }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Driver is inactive.");
            challans.Verify(x => x.AddAsync(
                It.IsAny<DeliveryChallan>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
