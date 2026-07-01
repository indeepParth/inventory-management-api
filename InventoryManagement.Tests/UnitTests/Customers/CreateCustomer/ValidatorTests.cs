using FluentAssertions;
using InventoryManagement.Application.Features.Customers.CreateCustomer;

namespace InventoryManagement.Tests.UnitTests.Customers.CreateCustomer
{
    public class ValidatorTests
    {
        [Fact]
        public void Validate_Should_Accept_Valid_Indian_Gstin()
        {
            var result = new Validator().Validate(ValidCommand());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_Should_Reject_Invalid_Gstin()
        {
            var command = ValidCommand();
            command.GstNumber = "invalid";

            var result = new Validator().Validate(command);

            result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.GstNumber));
        }

        [Fact]
        public void Validate_Should_Reject_Negative_Credit_Limit()
        {
            var command = ValidCommand();
            command.CreditLimit = -1;

            var result = new Validator().Validate(command);

            result.Errors.Should().Contain(x => x.PropertyName == nameof(Command.CreditLimit));
        }

        private static Command ValidCommand() => new()
        {
            Name = "Acme Retail",
            GstNumber = "27AAPFU0939F1ZV",
            CreditLimit = 10000
        };
    }
}
