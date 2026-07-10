using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Common.Behaviors
{
    public sealed class BusinessTransitionLoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private static readonly string[] TransitionNames =
        [
            ".PostPurchase.",
            ".CancelPurchase.",
            ".PostSalesInvoice.",
            ".CancelSalesInvoice.",
            ".PostDeliveryChallan.",
            ".CancelDeliveryChallan.",
            ".CreatePayment.",
            ".ReversePayment.",
            ".PostCustomerReturn.",
            ".CancelCustomerReturn.",
            ".PostSupplierReturn.",
            ".CancelSupplierReturn.",
            ".RecordDamage.",
            ".RecordAdjustment.",
            ".ReverseManualCorrection."
        ];

        private readonly ILogger<BusinessTransitionLoggingBehavior<TRequest, TResponse>> _logger;

        public BusinessTransitionLoggingBehavior(
            ILogger<BusinessTransitionLoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var response = await next();
            var requestName = request.GetType().FullName ?? request.GetType().Name;

            if (TryGetBusinessTransition(requestName, out var transitionName))
            {
                _logger.LogInformation(
                    "Business transition completed: {BusinessTransition}",
                    transitionName);
            }

            return response;
        }

        public static bool TryGetBusinessTransition(
            string requestName,
            out string transitionName)
        {
            foreach (var transition in TransitionNames)
            {
                if (requestName.Contains(
                        transition,
                        StringComparison.Ordinal))
                {
                    transitionName = transition.Trim('.');
                    return true;
                }
            }

            transitionName = string.Empty;
            return false;
        }
    }
}
