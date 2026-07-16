using FluentAssertions;
using InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.CreateDeliveryChallan
{
    public class ValidatorTests
    {
        [Fact]
        public void Validate_Should_Reject_Empty_Items_And_Nonpositive_Quantity()
        {
            var validator = new Validator();
            var empty = validator.Validate(new Command());
            var invalidItem = validator.Validate(new Command
            {
                ChallanNumber = "DC-1",
                CustomerId = 1,
                ChallanDate = DateTime.UtcNow,
                DeliveryFromAddress = "Warehouse",
                DeliveryAddress = "Address",
                Items = { new DeliveryChallanItemInput { ProductId = 1, Quantity = 0 } }
            });

            empty.Errors.Should().Contain(x => x.PropertyName == "Items");
            invalidItem.Errors.Should().Contain(x =>
                x.PropertyName == "Items[0].Quantity");
        }

        [Fact]
        public void Validate_Should_Reject_Missing_From_Address_And_Negative_Delivery_Charge()
        {
            var validator = new Validator();

            var result = validator.Validate(new Command
            {
                ChallanNumber = "DC-1",
                CustomerId = 1,
                ChallanDate = DateTime.UtcNow,
                DeliveryAddress = "Customer address",
                DeliveryCharge = -1,
                Items = { new DeliveryChallanItemInput { ProductId = 1, Quantity = 1 } }
            });

            result.Errors.Should().Contain(x => x.PropertyName == "DeliveryFromAddress");
            result.Errors.Should().Contain(x => x.PropertyName == "DeliveryCharge");
        }
    }
}
