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
                Mock.Of<IProductRepository>())
                .Handle(new Command(
                    1, "DC-1", 1, DateTime.UtcNow, null, null,
                    "Address", null,
                    new List<DeliveryChallanItemInput>
                    {
                        new() { ProductId = 1, Quantity = 1 }
                    }), CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Only Draft delivery challans may be edited.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
