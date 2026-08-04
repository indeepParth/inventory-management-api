using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.PostDeliveryChallan;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.PostDeliveryChallan
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Decrease_Stock_By_Converted_Base_Quantity()
        {
            var product = new Product
            {
                Id = 2, Name = "Sand", SKU = "SAND", Quantity = 10, AverageCost = 12.5m
            };
            var challan = Draft(
                product,
                unitId: 7,
                unitName: "Tractor",
                enteredQuantity: 2m,
                convertedBaseQuantity: 8m);
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();
            StockMovement? added = null;
            movements.Setup(x => x.AddAsync(
                    It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .Callback<StockMovement, CancellationToken>((x, _) => added = x)
                .Returns(Task.CompletedTask);
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(x => x.Username).Returns("dispatcher");

            var result = await new Handler(repository.Object, movements.Object, user.Object)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            result.Status.Should().Be(DeliveryChallanStatus.Posted);
            product.Quantity.Should().Be(2);
            added.Should().NotBeNull();
            added!.MovementType.Should().Be(StockMovementType.Sale);
            added.QuantityChange.Should().Be(-8);
            added.BalanceBefore.Should().Be(10);
            added.BalanceAfter.Should().Be(2);
            added.UnitCost.Should().Be(12.5m);
            result.Items.Should().ContainSingle(x =>
                x.EnteredQuantity == 2m &&
                x.UnitName == "Tractor" &&
                x.ConvertedBaseQuantity == 8m);
        }

        [Fact]
        public async Task Handle_Should_Reject_Insufficient_Aggregate_Stock()
        {
            var product = new Product
            {
                Id = 2, Name = "Product", SKU = "SKU", Quantity = 5
            };
            var challan = Draft(
                product,
                unitId: 1,
                unitName: "Ton",
                enteredQuantity: 2m,
                convertedBaseQuantity: 4m);
            challan.Items.Add(new DeliveryChallanItem
            {
                ProductId = product.Id,
                Product = product,
                EnteredQuantity = 0.5m,
                UnitId = 7,
                Unit = new Unit { Id = 7, Name = "Tractor", IsActive = true },
                ConvertedBaseQuantity = 2
            });
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();

            var action = () => new Handler(
                repository.Object, movements.Object, Mock.Of<ICurrentUserService>())
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Insufficient stock for product 2.");
            product.Quantity.Should().Be(5);
            movements.Verify(x => x.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Post_Repeated_Product_Lines_Using_Converted_Base_Quantities()
        {
            var product = new Product
            {
                Id = 2, Name = "Sand", SKU = "SAND", Quantity = 10, AverageCost = 12.5m
            };
            var challan = Draft(
                product,
                unitId: 7,
                unitName: "Tractor",
                enteredQuantity: 1m,
                convertedBaseQuantity: 4m);
            challan.Items.Add(new DeliveryChallanItem
            {
                ProductId = product.Id,
                Product = product,
                EnteredQuantity = 2,
                UnitId = 1,
                Unit = new Unit { Id = 1, Name = "Ton", IsActive = true },
                ConvertedBaseQuantity = 2
            });
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();
            var added = new List<StockMovement>();
            movements.Setup(x => x.AddAsync(
                    It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .Callback<StockMovement, CancellationToken>((x, _) => added.Add(x))
                .Returns(Task.CompletedTask);

            await new Handler(
                    repository.Object,
                    movements.Object,
                    Mock.Of<ICurrentUserService>())
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            product.Quantity.Should().Be(4);
            added.Should().HaveCount(2);
            added.Sum(x => x.QuantityChange).Should().Be(-6);
            added.Select(x => x.QuantityChange).Should().Contain(new[] { -4m, -2m });
        }

        [Fact]
        public async Task Handle_Should_Reject_Already_Posted_Challan()
        {
            var product = new Product { Id = 2, Name = "Product", SKU = "SKU", Quantity = 10 };
            var challan = Draft(
                product,
                unitId: 1,
                unitName: "Ton",
                enteredQuantity: 1m,
                convertedBaseQuantity: 1m);
            challan.Status = DeliveryChallanStatus.Posted;
            var repository = TransactionalRepository(challan);

            var action = () => new Handler(
                repository.Object,
                Mock.Of<IStockMovementRepository>(),
                Mock.Of<ICurrentUserService>())
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Only Draft delivery challans may be posted.");
            product.Quantity.Should().Be(10);
        }

        private static DeliveryChallan Draft(
            Product product,
            int unitId,
            string unitName,
            decimal enteredQuantity,
            decimal convertedBaseQuantity) => new()
        {
            Id = 1,
            ChallanNumber = "DC-1",
            Customer = new Customer { Id = 1, Name = "Customer" },
            Status = DeliveryChallanStatus.Draft,
            Items =
            {
                new DeliveryChallanItem
                {
                    ProductId = product.Id,
                    Product = product,
                    EnteredQuantity = enteredQuantity,
                    UnitId = unitId,
                    Unit = new Unit { Id = unitId, Name = unitName, IsActive = true },
                    ConvertedBaseQuantity = convertedBaseQuantity
                }
            }
        };

        private static Mock<IDeliveryChallanRepository> TransactionalRepository(
            DeliveryChallan challan)
        {
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>(
                    (operation, token) => operation(token));
            repository.Setup(x => x.GetForUpdateAsync(
                    challan.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challan);
            repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return repository;
        }
    }
}
