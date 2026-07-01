using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Customers.DeactivateCustomer;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Customers.DeactivateCustomer
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Deactivate_Customer_And_Update_Timestamp()
        {
            var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
            var customer = new Customer
            {
                Id = 1,
                Name = "Acme",
                IsActive = true,
                UpdatedAtUtc = originalUpdatedAt
            };
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            repository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var response = await new Handler(repository.Object).Handle(
                new Command { Id = 1 },
                CancellationToken.None);

            response.IsActive.Should().BeFalse();
            customer.UpdatedAtUtc.Should().BeAfter(originalUpdatedAt);
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Safely_Return_Already_Inactive_Customer()
        {
            var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
            var customer = new Customer
            {
                Id = 1,
                Name = "Acme",
                IsActive = false,
                UpdatedAtUtc = originalUpdatedAt
            };
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var response = await new Handler(repository.Object).Handle(
                new Command { Id = 1 },
                CancellationToken.None);

            response.IsActive.Should().BeFalse();
            customer.UpdatedAtUtc.Should().Be(originalUpdatedAt);
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Reject_Unknown_Customer()
        {
            var repository = new Mock<ICustomerRepository>();

            var action = () => new Handler(repository.Object).Handle(
                new Command { Id = 404 },
                CancellationToken.None);

            await action.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Customer not found.");
        }
    }
}
