using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;

        public Handler(
            ISalesInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository,
            ICurrentUserService currentUserService)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            if (request.Items.Any(x => x.DeliveryChallanItemId.HasValue))
            {
                throw new BadRequestException(
                    "Use the challan invoice operation for delivery challan items.");
            }

            var invoiceNumber = request.InvoiceNumber.Trim();
            if (await _invoiceRepository.InvoiceNumberExistsAsync(
                invoiceNumber,
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

            var now = DateTime.UtcNow;
            var invoice = new SalesInvoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = customer.Id,
                Customer = customer,
                InvoiceDate = request.InvoiceDate,
                Status = SalesInvoiceStatus.Draft,
                Discount = request.Discount,
                OtherCharges = request.OtherCharges,
                AmountPaid = 0,
                Notes = NormalizeOptional(request.Notes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = _currentUserService.Username
            };

            foreach (var input in request.Items)
            {
                var product = await _productRepository.GetProductByIdAsync(
                    input.ProductId,
                    cancellationToken);
                if (product is null)
                {
                    throw new NotFoundException($"Product {input.ProductId} not found.");
                }

                var lineSubtotal = RoundMoney(input.Quantity * input.SellingUnitPrice);
                var taxAmount = RoundMoney(lineSubtotal * input.TaxRate / 100m);
                invoice.Items.Add(new SalesInvoiceItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity,
                    SellingUnitPrice = input.SellingUnitPrice,
                    TaxRate = input.TaxRate,
                    TaxAmount = taxAmount,
                    LineTotal = lineSubtotal + taxAmount,
                    CostAtSale = null
                });
                invoice.Subtotal += lineSubtotal;
                invoice.TaxAmount += taxAmount;
            }

            invoice.GrandTotal = RoundMoney(
                invoice.Subtotal -
                invoice.Discount +
                invoice.TaxAmount +
                invoice.OtherCharges);
            if (invoice.GrandTotal < 0)
            {
                throw new BadRequestException("Grand total cannot be negative.");
            }

            invoice.BalanceDue = invoice.GrandTotal;
            await _invoiceRepository.AddAsync(invoice, cancellationToken);
            await _invoiceRepository.SaveChangesAsync(cancellationToken);
            return invoice.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
