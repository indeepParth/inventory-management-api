using FluentValidation;
using InventoryManagement.Application.Common.Exceptions;
using MediatR;

namespace InventoryManagement.Application.Common.Behaviors
{
    public class ValidationBehavior <TRequest, TResponse> 
            : IPipelineBehavior<TRequest, TResponse> 
            where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var results = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = results
                    .SelectMany(e => e.Errors)
                    .Where(e => e != null)
                    .ToList();

                if (failures.Any())
                {
                    var errors = failures
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            e => e.Key,
                            e => e.Select(x => x.ErrorMessage).ToArray());

                    throw new InventoryManagement.Application.Common.Exceptions.ValidationException(errors);
                }
            }

            return await next();
        }
    }
}
