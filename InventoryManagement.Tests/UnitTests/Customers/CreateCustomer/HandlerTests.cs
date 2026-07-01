using FluentAssertions;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Customers.CreateCustomer;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Customers.CreateCustomer
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Normalize_And_Create_Customer()
        {
            Customer? addedCustomer = null;
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .Callback<Customer, CancellationToken>((customer, _) => addedCustomer = customer)
                .Returns(Task.CompletedTask);
            repository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var response = await new Handler(repository.Object).Handle(new Command
            {
                Name = "  Acme Retail  ",
                ContactPerson = "  Priya  ",
                GstNumber = "27aapfu0939f1zv",
                CreditLimit = 25000
            }, CancellationToken.None);

            addedCustomer.Should().NotBeNull();
            addedCustomer!.Name.Should().Be("Acme Retail");
            addedCustomer.ContactPerson.Should().Be("Priya");
            addedCustomer.GstNumber.Should().Be("27AAPFU0939F1ZV");
            addedCustomer.IsActive.Should().BeTrue();
            addedCustomer.CreatedAtUtc.Should().Be(addedCustomer.UpdatedAtUtc);
            response.Name.Should().Be(addedCustomer.Name);
            repository.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Reject_Duplicate_Name()
        {
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetByNameAsync("Acme Retail", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = 1, Name = "ACME RETAIL" });

            var action = () => new Handler(repository.Object).Handle(new Command
            {
                Name = "Acme Retail"
            }, CancellationToken.None);

            await action.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Customer name already exists.");
            repository.Verify(
                x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
