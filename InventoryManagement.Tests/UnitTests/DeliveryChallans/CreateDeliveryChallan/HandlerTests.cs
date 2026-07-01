using FluentAssertions;
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
                challans.Object, customers.Object, products.Object, currentUser.Object)
                .Handle(new Command
                {
                    ChallanNumber = " DC-1 ",
                    CustomerId = 1,
                    ChallanDate = new DateTime(2026, 7, 1),
                    DeliveryAddress = " Warehouse ",
                    Items = { new DeliveryChallanItemInput { ProductId = 2, Quantity = 1.5m } }
                }, CancellationToken.None);

            added.Should().NotBeNull();
            added!.Status.Should().Be(DeliveryChallanStatus.Draft);
            added.ChallanNumber.Should().Be("DC-1");
            added.DeliveryAddress.Should().Be("Warehouse");
            added.CreatedBy.Should().Be("dispatcher");
            response.Items.Should().ContainSingle(x => x.Quantity == 1.5m);
            product.Quantity.Should().Be(25);
        }
    }
}
