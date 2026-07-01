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
        public async Task Handle_Should_Decrease_Stock_And_Create_Sale_Movement()
        {
            var product = new Product
            {
                Id = 2, Name = "Product", SKU = "SKU", Quantity = 10, AverageCost = 12.5m
            };
            var challan = Draft(product, 3);
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
            product.Quantity.Should().Be(7);
            added.Should().NotBeNull();
            added!.MovementType.Should().Be(StockMovementType.Sale);
            added.QuantityChange.Should().Be(-3);
            added.BalanceBefore.Should().Be(10);
            added.BalanceAfter.Should().Be(7);
            added.UnitCost.Should().Be(12.5m);
        }

        [Fact]
        public async Task Handle_Should_Reject_Insufficient_Aggregate_Stock()
        {
            var product = new Product
            {
                Id = 2, Name = "Product", SKU = "SKU", Quantity = 5
            };
            var challan = Draft(product, 3);
            challan.Items.Add(new DeliveryChallanItem
            {
                ProductId = product.Id, Product = product, Quantity = 3
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
        public async Task Handle_Should_Reject_Already_Posted_Challan()
        {
            var product = new Product { Id = 2, Name = "Product", SKU = "SKU", Quantity = 10 };
            var challan = Draft(product, 1);
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

        private static DeliveryChallan Draft(Product product, decimal quantity) => new()
        {
            Id = 1,
            ChallanNumber = "DC-1",
            Customer = new Customer { Id = 1, Name = "Customer" },
            Status = DeliveryChallanStatus.Draft,
            Items =
            {
                new DeliveryChallanItem
                {
                    ProductId = product.Id, Product = product, Quantity = quantity
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
