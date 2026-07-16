using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.UpdateDeliveryChallan;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.UpdateDeliveryChallan
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Reject_NonDraft_Challan()
        {
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetForUpdateAsync(
                    1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryChallan
                {
                    Id = 1, Status = DeliveryChallanStatus.Posted
                });

            var action = () => new Handler(
                repository.Object,
                Mock.Of<ICustomerRepository>(),
                Mock.Of<IDriverRepository>(),
                Mock.Of<IProductRepository>())
                .Handle(new Command(
                    1, "DC-1", 1, DateTime.UtcNow, null, null, null,
                    "Warehouse", "Address", 0, null,
                    new List<DeliveryChallanItemInput>
                    {
                        new() { ProductId = 1, Quantity = 1 }
                    }), CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Only Draft delivery challans may be edited.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Driver()
        {
            var product = new Product { Id = 2, Name = "Product", SKU = "SKU" };
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetForUpdateAsync(
                    1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeliveryChallan
                {
                    Id = 1,
                    Status = DeliveryChallanStatus.Draft,
                    Customer = new Customer { Id = 1, Name = "Customer" },
                    Items =
                    {
                        new DeliveryChallanItem
                        {
                            DeliveryChallanId = 1,
                            ProductId = product.Id,
                            Product = product,
                            Quantity = 1
                        }
                    }
                });
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Customer", IsActive = true });
            var drivers = new Mock<IDriverRepository>();
            drivers.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Driver { Id = 3, Name = "Driver", IsActive = false });

            var action = () => new Handler(
                repository.Object,
                customers.Object,
                drivers.Object,
                Mock.Of<IProductRepository>())
                .Handle(new Command(
                    1,
                    "DC-1",
                    1,
                    new DateTime(2026, 7, 1),
                    null,
                    3,
                    null,
                    "Warehouse",
                    "Customer site",
                    0,
                    null,
                    new List<DeliveryChallanItemInput>
                    {
                        new() { ProductId = 2, Quantity = 1 }
                    }), CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Driver is inactive.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
