using FluentAssertions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Customers.GetCustomers;
using InventoryManagement.Domain.Entities;
using Moq;

namespace InventoryManagement.Tests.UnitTests.Customers.GetCustomers
{
    public class HandlerTests
    {
        [Fact]
        public async Task Handle_Should_Return_Paged_Response()
        {
            var repository = new Mock<ICustomerRepository>();
            repository
                .Setup(x => x.GetAllAsync(2, 5, "acme", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Customer>
                {
                    new() { Id = 6, Name = "Acme Retail", IsActive = true }
                });
            repository
                .Setup(x => x.GetCountAsync("acme", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(8);

            var response = await new Handler(repository.Object).Handle(new Query
            {
                PageNumber = 2,
                PageSize = 5,
                Search = "acme",
                IsActive = true
            }, CancellationToken.None);

            response.Items.Should().ContainSingle(x => x.Name == "Acme Retail");
            response.PageNumber.Should().Be(2);
            response.PageSize.Should().Be(5);
            response.TotalCount.Should().Be(8);
            response.TotalPages.Should().Be(2);
            response.HasPreviousPage.Should().BeTrue();
            response.HasNextPage.Should().BeFalse();
        }
    }
}
