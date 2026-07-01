using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Purchases.UpdatePurchase;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Purchases.UpdatePurchase
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Replace_Items_And_Recalculate_Totals()
        {
            var purchase = CreatePurchase(PurchaseStatus.Draft);
            var purchaseRepository = new Mock<IPurchaseRepository>();
            purchaseRepository
                .Setup(x => x.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(purchase);
            purchaseRepository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var supplierRepository = new Mock<ISupplierRepository>();
            supplierRepository
                .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Supplier { Id = 2, Name = "New supplier", IsActive = true });

            var productRepository = new Mock<IProductRepository>();
            productRepository
                .Setup(x => x.GetProductByIdAsync(20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 20, Name = "New product", SKU = "NEW-1" });

            var response = await new Handler(
                purchaseRepository.Object,
                supplierRepository.Object,
                productRepository.Object).Handle(new Command(
                    5,
                    " PUR-UPDATED ",
                    2,
                    " BILL-UPDATED ",
                    new DateTime(2026, 7, 2),
                    2,
                    1,
                    " updated ",
                    new List<PurchaseItemInput>
                    {
                        new()
                        {
                            ProductId = 20,
                            Quantity = 3,
                            UnitCost = 10,
                            TaxRate = 18
                        }
                    }),
                    CancellationToken.None);

            purchase.PurchaseNumber.Should().Be("PUR-UPDATED");
            purchase.SupplierId.Should().Be(2);
            purchase.Items.Should().ContainSingle(x => x.ProductId == 20);
            purchase.Subtotal.Should().Be(30);
            purchase.TaxAmount.Should().Be(5.40m);
            purchase.GrandTotal.Should().Be(34.40m);
            purchase.CreatedAtUtc.Should().Be(new DateTime(2026, 7, 1));
            purchase.CreatedBy.Should().Be("creator");
            response.SupplierName.Should().Be("New supplier");
            purchaseRepository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(PurchaseStatus.Posted)]
        [InlineData(PurchaseStatus.Cancelled)]
        public async Task Handle_Should_Reject_NonDraft_Purchase(PurchaseStatus status)
        {
            var purchaseRepository = new Mock<IPurchaseRepository>();
            purchaseRepository
                .Setup(x => x.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePurchase(status));

            var action = () => new Handler(
                purchaseRepository.Object,
                Mock.Of<ISupplierRepository>(),
                Mock.Of<IProductRepository>()).Handle(
                    ValidCommand(),
                    CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Only Draft purchases may be edited.");
            purchaseRepository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Purchase CreatePurchase(PurchaseStatus status)
        {
            return new Purchase
            {
                Id = 5,
                PurchaseNumber = "PUR-OLD",
                SupplierId = 1,
                Supplier = new Supplier { Id = 1, Name = "Old supplier" },
                Status = status,
                CreatedAtUtc = new DateTime(2026, 7, 1),
                CreatedBy = "creator",
                Items =
                {
                    new PurchaseItem
                    {
                        ProductId = 10,
                        Product = new Product { Id = 10, Name = "Old product", SKU = "OLD-1" },
                        Quantity = 1,
                        UnitCost = 5,
                        LineTotal = 5
                    }
                }
            };
        }

        private static Command ValidCommand() => new(
            5,
            "PUR-UPDATED",
            2,
            null,
            new DateTime(2026, 7, 2),
            0,
            0,
            null,
            new List<PurchaseItemInput>
            {
                new() { ProductId = 20, Quantity = 1, UnitCost = 10 }
            });
    }
}
