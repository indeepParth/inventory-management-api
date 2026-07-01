using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.CancelDeliveryChallan;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.CancelDeliveryChallan
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Cancel_Draft_Without_Stock_Movements()
        {
            var challan = Challan(DeliveryChallanStatus.Draft);
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();

            var result = await CreateHandler(repository, movements)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            result.Status.Should().Be(DeliveryChallanStatus.Cancelled);
            challan.CancelledAtUtc.Should().NotBeNull();
            movements.Verify(x => x.AddAsync(
                It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Restore_Posted_Stock_And_Keep_Original_Movement()
        {
            var challan = Challan(DeliveryChallanStatus.Posted);
            var product = new Product
            {
                Id = 2, Name = "Product", SKU = "SKU", Quantity = 7, AverageCost = 12
            };
            var original = new StockMovement
            {
                Id = 10,
                ProductId = product.Id,
                Product = product,
                MovementType = StockMovementType.Sale,
                QuantityChange = -3,
                UnitCost = 12
            };
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();
            movements.Setup(x => x.GetDeliveryChallanMovementsForUpdateAsync(
                    1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StockMovement> { original });
            StockMovement? reversal = null;
            movements.Setup(x => x.AddAsync(
                    It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .Callback<StockMovement, CancellationToken>((x, _) => reversal = x)
                .Returns(Task.CompletedTask);

            var result = await CreateHandler(repository, movements)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            result.Status.Should().Be(DeliveryChallanStatus.Cancelled);
            product.Quantity.Should().Be(10);
            original.MovementType.Should().Be(StockMovementType.Sale);
            reversal.Should().NotBeNull();
            reversal!.MovementType.Should().Be(StockMovementType.Reversal);
            reversal.QuantityChange.Should().Be(3);
            reversal.BalanceBefore.Should().Be(7);
            reversal.BalanceAfter.Should().Be(10);
            reversal.UnitCost.Should().Be(12);
        }

        [Fact]
        public async Task Handle_Should_Be_Idempotent_When_Already_Cancelled()
        {
            var challan = Challan(DeliveryChallanStatus.Cancelled);
            challan.CancelledAtUtc = new DateTime(2026, 7, 1);
            var repository = TransactionalRepository(challan);
            var movements = new Mock<IStockMovementRepository>();

            var result = await CreateHandler(repository, movements)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            result.CancelledAtUtc.Should().Be(new DateTime(2026, 7, 1));
            movements.Verify(x => x.GetDeliveryChallanMovementsForUpdateAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            repository.Verify(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Invoiced_Challan()
        {
            var repository = TransactionalRepository(
                Challan(DeliveryChallanStatus.Invoiced));

            var action = () => CreateHandler(
                    repository, new Mock<IStockMovementRepository>())
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Invoiced delivery challans cannot be cancelled directly.");
        }

        private static Handler CreateHandler(
            Mock<IDeliveryChallanRepository> repository,
            Mock<IStockMovementRepository> movements)
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(x => x.Username).Returns("dispatcher");
            return new Handler(repository.Object, movements.Object, user.Object);
        }

        private static DeliveryChallan Challan(DeliveryChallanStatus status) => new()
        {
            Id = 1,
            ChallanNumber = "DC-1",
            Customer = new Customer { Id = 1, Name = "Customer" },
            Status = status
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
