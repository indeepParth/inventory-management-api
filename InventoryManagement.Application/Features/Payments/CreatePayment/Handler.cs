using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Payments.CreatePayment
{
    public class Handler : IRequestHandler<Command, PaymentResponse>
    {
        private readonly IPaymentRepository _payments;
        private readonly ICurrentUserService _currentUser;

        public Handler(IPaymentRepository payments, ICurrentUserService currentUser)
        {
            _payments = payments;
            _currentUser = currentUser;
        }

        public async Task<PaymentResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            Payment? created = null;
            await _payments.ExecuteInTransactionAsync(async transactionToken =>
            {
                var receiptNumber = request.ReceiptNumber.Trim();
                if (await _payments.ReceiptNumberExistsAsync(receiptNumber, transactionToken))
                    throw new BadRequestException("Receipt number already exists.");

                Customer? customer = null;
                SalesInvoice? invoice = null;
                Supplier? supplier = null;
                Purchase? purchase = null;
                if (request.CustomerId.HasValue)
                {
                    customer = await _payments.GetCustomerForUpdateAsync(
                        request.CustomerId.Value, transactionToken) ??
                        throw new NotFoundException("Customer not found.");
                    if (request.SalesInvoiceId.HasValue)
                    {
                        invoice = await _payments.GetInvoiceForUpdateAsync(
                            request.SalesInvoiceId.Value, transactionToken) ??
                            throw new NotFoundException("Sales invoice not found.");
                        if (invoice.CustomerId != customer.Id)
                            throw new BadRequestException(
                                "Sales invoice does not belong to the customer.");
                        if (invoice.Status is not (SalesInvoiceStatus.Posted or
                            SalesInvoiceStatus.PartiallyPaid))
                            throw new BadRequestException(
                                "Payments may only be applied to an unpaid posted invoice.");
                        if (request.Amount > invoice.BalanceDue)
                            throw new BadRequestException(
                                "Payment exceeds invoice balance due.");
                    }
                    else if (request.Amount > customer.BalanceDue)
                    {
                        throw new BadRequestException(
                            "Payment exceeds customer balance due.");
                    }
                }
                else
                {
                    supplier = await _payments.GetSupplierForUpdateAsync(
                        request.SupplierId!.Value, transactionToken) ??
                        throw new NotFoundException("Supplier not found.");
                    if (request.PurchaseId.HasValue)
                    {
                        purchase = await _payments.GetPurchaseForUpdateAsync(
                            request.PurchaseId.Value, transactionToken) ??
                            throw new NotFoundException("Purchase not found.");
                        if (purchase.SupplierId != supplier.Id)
                            throw new BadRequestException(
                                "Purchase does not belong to the supplier.");
                        if (purchase.Status is not (PurchaseStatus.Posted or
                            PurchaseStatus.PartiallyPaid))
                            throw new BadRequestException(
                                "Payments may only be applied to an unpaid posted purchase.");
                        if (request.Amount > purchase.BalanceDue)
                            throw new BadRequestException(
                                "Payment exceeds purchase balance due.");
                    }
                }

                var now = DateTime.UtcNow;
                created = new Payment
                {
                    ReceiptNumber = receiptNumber,
                    CustomerId = customer?.Id,
                    Customer = customer,
                    SalesInvoiceId = invoice?.Id,
                    SalesInvoice = invoice,
                    SupplierId = supplier?.Id,
                    Supplier = supplier,
                    PurchaseId = purchase?.Id,
                    Purchase = purchase,
                    PaymentDate = request.PaymentDate,
                    Amount = request.Amount,
                    Method = request.Method,
                    ExternalReference = Normalize(request.ExternalReference),
                    Note = Normalize(request.Note),
                    CreatedAtUtc = now,
                    CreatedBy = _currentUser.Username
                };
                await _payments.AddAsync(created, transactionToken);

                if (customer != null)
                {
                    customer.BalanceDue -= request.Amount;
                    customer.UpdatedAtUtc = now;
                }
                if (invoice != null)
                {
                    invoice.AmountPaid += request.Amount;
                    invoice.BalanceDue -= request.Amount;
                    invoice.Status = invoice.BalanceDue == 0
                        ? SalesInvoiceStatus.Paid
                        : SalesInvoiceStatus.PartiallyPaid;
                    invoice.UpdatedAtUtc = now;
                }
                if (purchase != null)
                {
                    purchase.AmountPaid += request.Amount;
                    purchase.BalanceDue -= request.Amount;
                    purchase.Status = purchase.BalanceDue == 0
                        ? PurchaseStatus.Paid
                        : PurchaseStatus.PartiallyPaid;
                }
                await _payments.SaveChangesAsync(transactionToken);
            }, cancellationToken);

            return created!.ToResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
