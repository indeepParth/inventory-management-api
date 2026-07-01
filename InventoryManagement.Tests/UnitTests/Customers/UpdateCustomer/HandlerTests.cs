using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Customers.UpdateCustomer;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Customers.UpdateCustomer
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Update_Fields_And_Preserve_CreatedAtUtc()
        {
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var customer = new Customer
            {
                Id = 1,
                Name = "Old name",
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt
            };
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            repository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var response = await new Handler(repository.Object).Handle(new Command(
                1,
                "  New name  ",
                "  Priya  ",
                "9876543210",
                "priya@example.com",
                "Mumbai",
                "Pune",
                "27aapfu0939f1zv",
                25000,
                false), CancellationToken.None);

            customer.Name.Should().Be("New name");
            customer.ContactPerson.Should().Be("Priya");
            customer.GstNumber.Should().Be("27AAPFU0939F1ZV");
            customer.CreatedAtUtc.Should().Be(createdAt);
            customer.UpdatedAtUtc.Should().BeAfter(createdAt);
            response.IsActive.Should().BeFalse();
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Reject_Name_Used_By_Another_Customer()
        {
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "First" });
            repository
                .Setup(x => x.GetByNameAsync("Second", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 2, Name = "SECOND" });

            var action = () => new Handler(repository.Object).Handle(new Command(
                1, "Second", null, null, null, null, null, null, 0, true),
                CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Customer name already exists.");
        }

        [Fact]
        public async Task Handle_Should_Allow_Current_Customers_Name_And_Gstin()
        {
            var customer = new Customer
            {
                Id = 1,
                Name = "Acme",
                GstNumber = "27AAPFU0939F1ZV"
            };
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            repository
                .Setup(x => x.GetByNameAsync("Acme", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            repository
                .Setup(x => x.GetByGstNumberAsync(
                    "27AAPFU0939F1ZV",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            repository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var action = () => new Handler(repository.Object).Handle(new Command(
                1, "Acme", null, null, null, null, null, "27AAPFU0939F1ZV", 0, true),
                CancellationToken.None);

            await action.Should().NotThrowAsync();
        }
    }
}
