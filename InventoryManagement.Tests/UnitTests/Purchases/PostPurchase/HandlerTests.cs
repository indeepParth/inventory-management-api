using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Purchases.PostPurchase;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Purchases.PostPurchase
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Post_Multiple_And_Repeated_Product_Lines()
        {
            var repeatedProduct = new Product
            {
                Id = 10,
                Name = "Steel",
                SKU = "ST-1",
                Quantity = 10,
                AverageCost = 20
            };
            var otherProduct = new Product
            {
                Id = 11,
                Name = "Cement",
                SKU = "CM-1",
                Quantity = 0,
                AverageCost = 0
            };
            var purchase = CreatePurchase(repeatedProduct, otherProduct);
            var movements = new List<StockMovement>();
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();
            movementRepository
                .Setup(x => x.AddAsync(
                    It.IsAny<StockMovement>(),
                    It.IsAny<CancellationToken>()))
                .Callback<StockMovement, CancellationToken>(
                    (movement, _) => movements.Add(movement))
                .Returns(Task.CompletedTask);
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.Username).Returns("buyer");

            var response = await new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                currentUser.Object).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            response.Status.Should().Be(PurchaseStatus.Posted);
            response.PostedAtUtc.Should().NotBeNull();
            repeatedProduct.Quantity.Should().Be(20);
            repeatedProduct.AverageCost.Should().Be(20);
            otherProduct.Quantity.Should().Be(2);
            otherProduct.AverageCost.Should().Be(50);
            movements.Should().HaveCount(3);
            movements[0].BalanceBefore.Should().Be(10);
            movements[0].BalanceAfter.Should().Be(15);
            movements[1].BalanceBefore.Should().Be(15);
            movements[1].BalanceAfter.Should().Be(20);
            movements.Should().OnlyContain(x =>
                x.MovementType == StockMovementType.Purchase &&
                x.SourceType == "Purchase" &&
                x.SourceId == "5" &&
                x.CreatedBy == "buyer");
        }

        [Theory]
        [InlineData(PurchaseStatus.Posted)]
        [InlineData(PurchaseStatus.Cancelled)]
        public async Task Handle_Should_Reject_NonDraft_Without_Adding_Movements(
            PurchaseStatus status)
        {
            var purchase = CreatePurchase(
                new Product { Id = 10, Name = "Steel", SKU = "ST-1" },
                new Product { Id = 11, Name = "Cement", SKU = "CM-1" });
            purchase.Status = status;
            var purchaseRepository = TransactionalRepository(purchase);
            var movementRepository = new Mock<IStockMovementRepository>();

            var action = () => new Handler(
                purchaseRepository.Object,
                movementRepository.Object,
                Mock.Of<ICurrentUserService>()).Handle(
                    new Command { Id = purchase.Id },
                    CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Only Draft purchases may be posted.");
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

        private static Purchase CreatePurchase(Product repeatedProduct, Product otherProduct)
        {
            return new Purchase
            {
                Id = 5,
                PurchaseNumber = "PUR-5",
                SupplierId = 1,
                Supplier = new Supplier { Id = 1, Name = "Supplier" },
                Status = PurchaseStatus.Draft,
                Items =
                {
                    Item(repeatedProduct, 5, 30),
                    Item(repeatedProduct, 5, 10),
                    Item(otherProduct, 2, 50)
                }
            };
        }

        private static PurchaseItem Item(Product product, decimal quantity, decimal unitCost)
        {
            return new PurchaseItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = quantity,
                UnitCost = unitCost
            };
        }
    }
}
