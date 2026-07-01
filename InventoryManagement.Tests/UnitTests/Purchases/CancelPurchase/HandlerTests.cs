using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Purchases.CancelPurchase;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Purchases.CancelPurchase
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Cancel_Draft_Without_Stock_Changes()
        {
            var purchase = PurchaseWithStatus(PurchaseStatus.Draft);
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();

            var response = await new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                Mock.Of<ICurrentUserService>()).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            response.Status.Should().Be(PurchaseStatus.Cancelled);
            response.CancelledAtUtc.Should().NotBeNull();
            movementRepository.Verify(
                x => x.GetPurchaseMovementsForUpdateAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            movementRepository.Verify(
                x => x.AddAsync(
                    It.IsAny<StockMovement>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reverse_Posted_Movements_And_Restore_Cost()
        {
            var purchase = PurchaseWithStatus(PurchaseStatus.Posted);
            var product = purchase.Items.First().Product;
            product.Quantity = 20;
            product.AverageCost = 20;
            var movements = new List<StockMovement>
            {
                PurchaseMovement(2, product, 5, 10),
                PurchaseMovement(1, product, 5, 30)
            };
            var reversals = new List<StockMovement>();
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();
            movementRepository
                .Setup(x => x.GetPurchaseMovementsForUpdateAsync(
                    purchase.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(movements);
            movementRepository
                .Setup(x => x.AddAsync(
                    It.IsAny<StockMovement>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StockMovement, CancellationToken>(
                    (movement, _) => reversals.Add(movement))
                .Returns(Task.CompletedTask);
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.Username).Returns("buyer");

            await new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                currentUser.Object).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            product.Quantity.Should().Be(10);
            product.AverageCost.Should().Be(20);
            reversals.Should().HaveCount(2);
            reversals[0].BalanceBefore.Should().Be(20);
            reversals[0].BalanceAfter.Should().Be(15);
            reversals[1].BalanceBefore.Should().Be(15);
            reversals[1].BalanceAfter.Should().Be(10);
            reversals.Should().OnlyContain(x =>
                x.MovementType == StockMovementType.Reversal &&
                x.QuantityChange == -5 &&
                x.SourceType == "PurchaseCancellation" &&
                x.SourceId == "5" &&
                x.CreatedBy == "buyer");
        }

        [Fact]
        public async Task Handle_Should_Reject_When_Remaining_Stock_Is_Insufficient()
        {
            var purchase = PurchaseWithStatus(PurchaseStatus.Posted);
            var product = purchase.Items.First().Product;
            product.Quantity = 4;
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();
            movementRepository
                .Setup(x => x.GetPurchaseMovementsForUpdateAsync(
                    purchase.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StockMovement>
                {
                    PurchaseMovement(1, product, 5, 30)
                });

            var action = () => new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                Mock.Of<ICurrentUserService>()).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Insufficient stock to cancel purchase for product 10.");
            purchase.Status.Should().Be(PurchaseStatus.Posted);
            product.Quantity.Should().Be(4);
            movementRepository.Verify(
                x => x.AddAsync(
                    It.IsAny<StockMovement>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Return_Already_Cancelled_Purchase_Without_Changes()
        {
            var purchase = PurchaseWithStatus(PurchaseStatus.Cancelled);
            purchase.CancelledAtUtc = new DateTime(2026, 7, 1);
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();

            var response = await new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                Mock.Of<ICurrentUserService>()).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            response.Status.Should().Be(PurchaseStatus.Cancelled);
            response.CancelledAtUtc.Should().Be(new DateTime(2026, 7, 1));
            purchaseRepository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            movementRepository.Verify(
                x => x.AddAsync(
                    It.IsAny<StockMovement>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Mock<IPurchaseRepository> TransactionalRepository(Purchase purchase)
        {
            var repository = new Mock<IPurchaseRepository>();
            repository
                .Setup(x => x.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<CancellationToken, Task> operation, CancellationToken token) =>
                    operation(token));
            repository
                .Setup(x => x.GetForUpdateAsync(
                    purchase.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(purchase);
            repository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return repository;
        }

        private static Purchase PurchaseWithStatus(PurchaseStatus status)
        {
            var product = new Product { Id = 10, Name = "Steel", SKU = "ST-1" };

            return new Purchase
            {
                Id = 5,
                PurchaseNumber = "PUR-5",
                SupplierId = 1,
                Supplier = new Supplier { Id = 1, Name = "Supplier" },
                Status = status,
                Items =
                {
                    new PurchaseItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = 5,
                        UnitCost = 30
                    }
                }
            };
        }

        private static StockMovement PurchaseMovement(
            int id,
            Product product,
            decimal quantity,
            decimal unitCost)
        {
            return new StockMovement
            {
                Id = id,
                ProductId = product.Id,
                Product = product,
                MovementType = StockMovementType.Purchase,
                QuantityChange = quantity,
                UnitCost = unitCost
            };
        }
    }
}
