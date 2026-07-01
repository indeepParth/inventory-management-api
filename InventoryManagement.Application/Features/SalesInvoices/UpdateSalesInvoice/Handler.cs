using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;

        public Handler(
            ISalesInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var invoice = await _invoiceRepository.GetForUpdateAsync(
                request.Id,
                cancellationToken) ??
                throw new NotFoundException("Sales invoice not found.");

            if (invoice.Status != SalesInvoiceStatus.Draft)
            {
                throw new BadRequestException(
                    "Only Draft sales invoices may be edited.");
            }

            var invoiceNumber = request.InvoiceNumber.Trim();
            if (await _invoiceRepository.InvoiceNumberExistsForOtherAsync(
                invoiceNumber,
                invoice.Id,
                cancellationToken))
            {
                throw new BadRequestException("Invoice number already exists.");
            }

            var customer = await _customerRepository.GetByIdAsync(
                request.CustomerId,
                cancellationToken);
            if (customer is null)
            {
                throw new NotFoundException("Customer not found.");
            }

            if (!customer.IsActive)
            {
                throw new BadRequestException("Customer is inactive.");
            }

            var replacementItems = new List<SalesInvoiceItem>();
            decimal subtotal = 0;
            decimal taxAmountTotal = 0;

            foreach (var input in request.Items)
            {
                var product = await _productRepository.GetProductByIdAsync(
                    input.ProductId,
                    cancellationToken);
                if (product is null)
                {
                    throw new NotFoundException($"Product {input.ProductId} not found.");
                }

                DeliveryChallanItem? challanItem = null;
                if (input.DeliveryChallanItemId.HasValue)
                {
                    challanItem = await _invoiceRepository.GetDeliveryChallanItemAsync(
                        input.DeliveryChallanItemId.Value,
                        cancellationToken);
                    if (challanItem is null)
                    {
                        throw new NotFoundException(
                            $"Delivery challan item {input.DeliveryChallanItemId.Value} not found.");
                    }

                    if (challanItem.ProductId != product.Id)
                    {
                        throw new BadRequestException(
                            "Delivery challan item product does not match invoice item product.");
                    }
                }

                var lineSubtotal = RoundMoney(input.Quantity * input.SellingUnitPrice);
                var taxAmount = RoundMoney(lineSubtotal * input.TaxRate / 100m);
                replacementItems.Add(new SalesInvoiceItem
                {
                    SalesInvoiceId = invoice.Id,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity,
                    SellingUnitPrice = input.SellingUnitPrice,
                    TaxRate = input.TaxRate,
                    TaxAmount = taxAmount,
                    LineTotal = lineSubtotal + taxAmount,
                    CostAtSale = null,
                    DeliveryChallanItemId = challanItem?.Id,
                    DeliveryChallanItem = challanItem
                });
                subtotal += lineSubtotal;
                taxAmountTotal += taxAmount;
            }

            var grandTotal = RoundMoney(
                subtotal -
                request.Discount +
                taxAmountTotal +
                request.OtherCharges);
            if (grandTotal < 0)
            {
                throw new BadRequestException("Grand total cannot be negative.");
            }

            invoice.InvoiceNumber = invoiceNumber;
            invoice.CustomerId = customer.Id;
            invoice.Customer = customer;
            invoice.InvoiceDate = request.InvoiceDate;
            invoice.Discount = request.Discount;
            invoice.OtherCharges = request.OtherCharges;
            invoice.Notes = NormalizeOptional(request.Notes);
            invoice.Subtotal = subtotal;
            invoice.TaxAmount = taxAmountTotal;
            invoice.GrandTotal = grandTotal;
            invoice.BalanceDue = grandTotal;
            invoice.UpdatedAtUtc = DateTime.UtcNow;
            _invoiceRepository.RemoveItems(invoice.Items);
            invoice.Items.Clear();

            foreach (var item in replacementItems)
            {
                invoice.Items.Add(item);
            }

            await _invoiceRepository.SaveChangesAsync(cancellationToken);
            return invoice.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
