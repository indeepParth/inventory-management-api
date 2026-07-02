using FluentAssertions;
using PurchaseQuery =
    InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister.Query;
using PurchaseValidator =
    InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister.Validator;
using SalesQuery =
    InventoryManagement.Application.Features.InventoryReports.GetSalesRegister.Query;
using SalesValidator =
    InventoryManagement.Application.Features.InventoryReports.GetSalesRegister.Validator;

namespace InventoryManagement.Tests.UnitTests.InventoryReports;

public class RegisterValidatorTests
{
    [Fact]
    public void PurchaseRegister_Should_Reject_Inverted_Date_Range()
    {
        var result = new PurchaseValidator().Validate(new PurchaseQuery
        {
            FromDate = new DateTime(2026, 7, 2),
            ToDate = new DateTime(2026, 7, 1)
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "ToDate");
    }

    [Fact]
    public void SalesRegister_Should_Reject_Invalid_Product()
    {
        var result = new SalesValidator().Validate(new SalesQuery
        {
            ProductId = 0
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "ProductId");
    }
}
