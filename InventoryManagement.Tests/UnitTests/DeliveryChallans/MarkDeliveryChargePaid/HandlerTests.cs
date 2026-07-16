using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.DeliveryChallans.MarkDeliveryChargePaid;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Moq;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.MarkDeliveryChargePaid
{
    public class HandlerTests
    {
        [Theory]
        [InlineData(DeliveryChallanStatus.Posted)]
        [InlineData(DeliveryChallanStatus.Invoiced)]
        public async Task Handle_Should_Mark_Delivery_Charge_Paid_For_Allowed_Status(
            DeliveryChallanStatus status)
        {
            var challan = Challan(status, deliveryCharge: 50);
            var repository = Repository(challan);

            var result = await new Handler(repository.Object)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            challan.IsDeliveryChargePaid.Should().BeTrue();
            challan.UpdatedAtUtc.Should().NotBe(new DateTime(2026, 7, 1));
            result.IsDeliveryChargePaid.Should().BeTrue();
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Current_Response_When_Already_Paid()
        {
            var updatedAt = new DateTime(2026, 7, 1);
            var challan = Challan(
                DeliveryChallanStatus.Posted,
                deliveryCharge: 50,
                isPaid: true,
                updatedAtUtc: updatedAt);
            var repository = Repository(challan);

            var result = await new Handler(repository.Object)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            result.IsDeliveryChargePaid.Should().BeTrue();
            challan.UpdatedAtUtc.Should().Be(updatedAt);
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(DeliveryChallanStatus.Draft)]
        [InlineData(DeliveryChallanStatus.Cancelled)]
        public async Task Handle_Should_Reject_Blocked_Statuses(
            DeliveryChallanStatus status)
        {
            var repository = Repository(Challan(status, deliveryCharge: 50));

            var action = () => new Handler(repository.Object)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage(
                    "Only Posted or Invoiced delivery challans may have delivery charge marked paid.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Zero_Delivery_Charge()
        {
            var repository = Repository(
                Challan(DeliveryChallanStatus.Posted, deliveryCharge: 0));

            var action = () => new Handler(repository.Object)
                .Handle(new Command { Id = 1 }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Delivery charge must be greater than zero.");
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static DeliveryChallan Challan(
            DeliveryChallanStatus status,
            decimal deliveryCharge,
            bool isPaid = false,
            DateTime? updatedAtUtc = null) => new()
            {
                Id = 1,
                ChallanNumber = "DC-1",
                CustomerId = 2,
                Customer = new Customer { Id = 2, Name = "Customer" },
                Status = status,
                DeliveryFromAddress = "Warehouse",
                DeliveryAddress = "Customer site",
                DeliveryCharge = deliveryCharge,
                IsDeliveryChargePaid = isPaid,
                UpdatedAtUtc = updatedAtUtc ?? new DateTime(2026, 7, 1)
            };

        private static Mock<IDeliveryChallanRepository> Repository(
            DeliveryChallan challan)
        {
            var repository = new Mock<IDeliveryChallanRepository>();
            repository.Setup(x => x.GetForUpdateAsync(
                    challan.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(challan);
            repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return repository;
        }
    }
}
