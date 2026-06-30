using FluentAssertions;
using FluentValidation;
using InventoryManagement.Application.Common.Behaviors;
using MediatR;
using AppValidationException = InventoryManagement.Application.Common.Exceptions.ValidationException;

namespace InventoryManagement.Tests.UnitTests.Common.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_Should_Throw_Structured_ValidationException()
        {
            var validators = new List<IValidator<TestRequest>>
            {
                new TestRequestValidator()
            };

            var behavior = new ValidationBehavior<TestRequest, string>(validators);

            var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
                behavior.Handle(
                    new TestRequest(),
                    _ => Task.FromResult("ok"),
                    CancellationToken.None));

            exception.Message.Should().Be("Validation failed.");
            exception.Errors.Should().ContainKey(nameof(TestRequest.Name));
        }

        private class TestRequest : IRequest<string>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class TestRequestValidator : AbstractValidator<TestRequest>
        {
            public TestRequestValidator()
            {
                RuleFor(x => x.Name).NotEmpty();
            }
        }
    }
}
