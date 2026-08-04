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
        public async Task Handle_Should_Recalculate_Converted_Base_Quantity()
        {
            var product = new Product { Id = 2, Name = "Product", SKU = "SKU", BaseUnitId = 1 };
            var challan = new DeliveryChallan
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
                        EnteredQuantity = 1,
                        UnitId = 1,
                        Unit = new Unit { Id = 1, Name = "Ton", IsActive = true },
                        ConvertedBaseQuantity = 1
                    }
                }
            };
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetForUpdateAsync(
                    1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challan);
            repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "Customer", IsActive = true });
            var products = new Mock<IProductRepository>();
            products.Setup(x => x.GetProductByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            var units = new Mock<IUnitRepository>();
            units.Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit
                {
                    Id = 3,
                    Name = "Bag",
                    IsActive = true,
                    BaseUnitId = 1,
                    FactorToBaseUnit = 2,
                });

            var result = await new Handler(
                repository.Object,
                customers.Object,
                Mock.Of<IDriverRepository>(),
                products.Object,
                units.Object)
                .Handle(new Command(
                    1,
                    "DC-1",
                    1,
                    new DateTime(2026, 7, 1),
                    null,
                    null,
                    null,
                    "Warehouse",
                    "Customer site",
                    0,
                    null,
                    new List<DeliveryChallanItemInput>
                    {
                        new() { ProductId = 2, EnteredQuantity = 1.5m, UnitId = 3 }
                    }), CancellationToken.None);

            result.Items.Should().ContainSingle(x =>
                x.EnteredQuantity == 1.5m &&
                x.UnitId == 3 &&
                x.UnitName == "Bag" &&
                x.ConvertedBaseQuantity == 3m &&
                x.Quantity == 3m);
            repository.Verify(x => x.RemoveItems(It.IsAny<IEnumerable<DeliveryChallanItem>>()));
        }

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
                Mock.Of<IProductRepository>(),
                Mock.Of<IUnitRepository>())
                .Handle(new Command(
                    1, "DC-1", 1, DateTime.UtcNow, null, null, null,
                    "Warehouse", "Address", 0, null,
                    new List<DeliveryChallanItemInput>
                    {
                        new() { ProductId = 1, EnteredQuantity = 1, UnitId = 1 }
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
                            EnteredQuantity = 1,
                            UnitId = 1,
                            Unit = new Unit { Id = 1, Name = "Ton", IsActive = true },
                            ConvertedBaseQuantity = 1
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
                Mock.Of<IProductRepository>(),
                Mock.Of<IUnitRepository>())
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
                        new() { ProductId = 2, EnteredQuantity = 1, UnitId = 1 }
                    }), CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Driver is inactive.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
