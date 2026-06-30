using FluentAssertions;
using InventoryManagement.Application.Features.Products.CreateProduct;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Tests.UnitTests.Products.CreateProduct;

public class ValidatorTests
{
    [Fact]
    public void Validate_Should_Reject_Missing_BaseUnit()
    {
        var command = ValidCommand();
        command.BaseUnit = default;

        var result = new Validator().Validate(command);

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.BaseUnit));
    }

    [Fact]
    public void Validate_Should_Accept_Fractional_Quantity()
    {
        var command = ValidCommand();
        command.Quantity = 1.125m;

        var result = new Validator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    private static Command ValidCommand() => new()
    {
        Name = "Cement",
        SKU = "CEMENT-001",
        Quantity = 1m,
        BaseUnit = UnitOfMeasure.Bag,
        DefaultSellingPrice = 425m,
        CategoryId = 1
    };
}
