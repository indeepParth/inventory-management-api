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
                DeliveryAddress = "Address",
                Items = { new DeliveryChallanItemInput { ProductId = 1, Quantity = 0 } }
            });

            empty.Errors.Should().Contain(x => x.PropertyName == "Items");
            invalidItem.Errors.Should().Contain(x =>
                x.PropertyName == "Items[0].Quantity");
        }
    }
}
