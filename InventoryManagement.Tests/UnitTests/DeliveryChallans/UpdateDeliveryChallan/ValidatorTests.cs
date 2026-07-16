using FluentAssertions;
using InventoryManagement.Application.Features.DeliveryChallans.UpdateDeliveryChallan;

namespace InventoryManagement.Tests.UnitTests.DeliveryChallans.UpdateDeliveryChallan
{
    public class ValidatorTests
    {
        [Fact]
        public void Validate_Should_Reject_Missing_From_Address_And_Negative_Delivery_Charge()
        {
            var validator = new Validator();

            var result = validator.Validate(new Command(
                1,
                "DC-1",
                1,
                DateTime.UtcNow,
                null,
                null,
                null,
                "",
                "Customer address",
                -1,
                null,
                new List<DeliveryChallanItemInput>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }));

            result.Errors.Should().Contain(x => x.PropertyName == "DeliveryFromAddress");
            result.Errors.Should().Contain(x => x.PropertyName == "DeliveryCharge");
        }
    }
}
