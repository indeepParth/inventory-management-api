using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Purchases.CreatePurchase;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Purchases.CreatePurchase
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Calculate_Totals_And_Create_Draft()
        {
            Purchase? addedPurchase = null;
            var purchaseRepository = new Mock<IPurchaseRepository>();
            purchaseRepository
                .Setup(x => x.AddAsync(
                    It.IsAny<Purchase>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Purchase, CancellationToken>(
                    (purchase, _) => addedPurchase = purchase)
                .Returns(Task.CompletedTask);
            purchaseRepository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var supplierRepository = new Mock<ISupplierRepository>();
            supplierRepository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Supplier { Id = 1, Name = "Acme", IsActive = true });

            var productRepository = new Mock<IProductRepository>();
            productRepository
                .Setup(x => x.GetProductByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 10, Name = "Steel", SKU = "ST-1" });
            productRepository
                .Setup(x => x.GetProductByIdAsync(11, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product { Id = 11, Name = "Cement", SKU = "CM-1" });

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.Username).Returns("buyer");

            var response = await new Handler(
                purchaseRepository.Object,
                supplierRepository.Object,
                productRepository.Object,
                currentUser.Object).Handle(new Command
                {
                    PurchaseNumber = " PUR-100 ",
                    SupplierId = 1,
                    SupplierBillNumber = " BILL-9 ",
                    BillDate = new DateTime(2026, 7, 1),
                    Discount = 1,
                    OtherCharges = 2,
                    Items =
                    {
                        new PurchaseItemInput
                        {
                            ProductId = 10,
                            Quantity = 2.5m,
                            UnitCost = 10.005m,
                            TaxRate = 18
                        },
                        new PurchaseItemInput
                        {
                            ProductId = 11,
                            Quantity = 1,
                            UnitCost = 5,
                            TaxRate = 0
                        }
                    }
                }, CancellationToken.None);

            addedPurchase.Should().NotBeNull();
            addedPurchase!.PurchaseNumber.Should().Be("PUR-100");
            addedPurchase.SupplierBillNumber.Should().Be("BILL-9");
            addedPurchase.Status.Should().Be(PurchaseStatus.Draft);
            addedPurchase.Subtotal.Should().Be(30.01m);
            addedPurchase.TaxAmount.Should().Be(4.50m);
            addedPurchase.GrandTotal.Should().Be(35.51m);
            addedPurchase.CreatedBy.Should().Be("buyer");
            addedPurchase.Items.First().LineTotal.Should().Be(29.51m);
            response.SupplierName.Should().Be("Acme");
            response.Items.Should().Contain(x =>
                x.ProductName == "Steel" && x.ProductSku == "ST-1");
        }

        [Fact]
        public async Task Handle_Should_Reject_Inactive_Supplier()
        {
            var purchaseRepository = new Mock<IPurchaseRepository>();
            var supplierRepository = new Mock<ISupplierRepository>();
            supplierRepository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Supplier { Id = 1, IsActive = false });

            var action = () => new Handler(
                purchaseRepository.Object,
                supplierRepository.Object,
                Mock.Of<IProductRepository>(),
                Mock.Of<ICurrentUserService>()).Handle(
                    ValidCommand(),
                    CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Supplier is inactive.");
            purchaseRepository.Verify(
                x => x.AddAsync(It.IsAny<Purchase>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Product()
        {
            var purchaseRepository = new Mock<IPurchaseRepository>();
            var supplierRepository = new Mock<ISupplierRepository>();
            supplierRepository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Supplier { Id = 1, IsActive = true });

            var action = () => new Handler(
                purchaseRepository.Object,
                supplierRepository.Object,
                Mock.Of<IProductRepository>(),
                Mock.Of<ICurrentUserService>()).Handle(
                    ValidCommand(),
                    CancellationToken.None);

            await action.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Product 10 not found.");
            purchaseRepository.Verify(
                x => x.AddAsync(It.IsAny<Purchase>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Command ValidCommand() => new()
        {
            PurchaseNumber = "PUR-100",
            SupplierId = 1,
            BillDate = new DateTime(2026, 7, 1),
            Items =
            {
                new PurchaseItemInput
                {
                    ProductId = 10,
                    Quantity = 1,
                    UnitCost = 10
                }
            }
        };
    }
}
