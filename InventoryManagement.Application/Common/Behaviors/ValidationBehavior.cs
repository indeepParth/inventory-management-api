using FluentValidation;
using MediatR;

namespace InventoryManagement.Application.Common.Behaviors
{
    public class ValidationBehavior <TRequest, TResponse> 
            : IPipelineBehavior<TRequest, TResponse> 
            where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validator;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validator)
        {
            _validator = validator;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if(_validator.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var results = Task.WhenAll(
                    _validator.Select(v => v.ValidateAsync(context, cancellationToken))
                );

                var failurer = results.Result
                    .SelectMany(e => e.Errors)
                    .Where(e => e != null)
                    .ToList();

                if (failurer.Any())
                    throw new ValidationException(failurer);
            }

            return await next();
        }
    }
}