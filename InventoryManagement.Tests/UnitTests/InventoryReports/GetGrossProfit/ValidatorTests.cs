using FluentAssertions;
using InventoryManagement.Application.Features.InventoryReports.GetGrossProfit;

namespace InventoryManagement.Tests.UnitTests.InventoryReports.GetGrossProfit;

public class ValidatorTests
{
    [Fact]
    public void Validate_Should_Reject_Invalid_Ids_And_Reversed_Date_Range()
    {
        var result = new Validator().Validate(new Query
        {
            FromDate = new DateTime(2026, 2, 1),
            ToDate = new DateTime(2026, 1, 1),
            InvoiceId = 0,
            ProductId = 0,
            CategoryId = 0,
            CustomerId = 0
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(5);
    }
}
