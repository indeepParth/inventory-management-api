using FluentAssertions;
using InventoryManagement.Application.Features.Products.CreateProduct;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Tests.UnitTests.Products.CreateProduct;

public class ValidatorTests
{
    [Fact]
    public void Command_Should_Not_Expose_Quantity()
    {
        typeof(Command).GetProperty("Quantity").Should().BeNull();
    }

    [Fact]
    public void Validate_Should_Reject_Missing_BaseUnit()
    {
        var command = ValidCommand();
        command.BaseUnit = default;

        var result = new Validator().Validate(command);

        result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.BaseUnit));
    }

    private static Command ValidCommand() => new()
    {
        Name = "Cement",
        SKU = "CEMENT-001",
        BaseUnit = UnitOfMeasure.Bag,
        DefaultSellingPrice = 425m,
        CategoryId = 1
    };
}
