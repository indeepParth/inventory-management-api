using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products;
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
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDocumentNumberService _documentNumbers;

        public Handler(
            ISalesInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository,
            IStockMovementRepository stockMovementRepository,
            ICurrentUserService currentUserService,
            IDocumentNumberService documentNumbers)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUserService = currentUserService;
            _documentNumbers = documentNumbers;
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

            SalesInvoice? created = null;
            await _invoiceRepository.ExecuteInTransactionAsync(async transactionToken =>
            {
                var invoiceNumber = await _documentNumbers.GenerateAsync(
                    DocumentNumberType.SalesInvoice,
                    request.InvoiceDate,
                    isDirectInvoice: true,
                    cancellationToken: transactionToken);

                var customer = await _customerRepository.GetByIdAsync(
                    request.CustomerId,
                    transactionToken);
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
                        transactionToken);
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

                foreach (var group in invoice.Items.GroupBy(x =>
                    ProductStock.GetStockProduct(x.Product).Id))
                {
                    var stockProduct = ProductStock.GetStockProduct(group.First().Product);
                    var requiredQuantity = group.Sum(x =>
                        ProductStock.GetStockQuantity(x.Product, x.Quantity));
                    if (stockProduct.Quantity < requiredQuantity)
                    {
                        throw new BadRequestException(
                            $"Insufficient stock for product {stockProduct.Id}.");
                    }
                }

                invoice.BalanceDue = invoice.GrandTotal;
                invoice.AmountPaid = 0;
                await _invoiceRepository.AddAsync(invoice, transactionToken);
                await _invoiceRepository.SaveChangesAsync(transactionToken);

                var postedAtUtc = DateTime.UtcNow;
                foreach (var item in invoice.Items)
                {
                    var product = ProductStock.GetStockProduct(item.Product);
                    var stockQuantity = ProductStock.GetStockQuantity(
                        item.Product,
                        item.Quantity);
                    var balanceBefore = product.Quantity;
                    var costAtSale = ProductStock.GetCostAtSale(item.Product);
                    product.Quantity -= stockQuantity;
                    item.CostAtSale = costAtSale;

                    await _stockMovementRepository.AddAsync(
                        new StockMovement
                        {
                            ProductId = product.Id,
                            Product = product,
                            MovementType = StockMovementType.Sale,
                            QuantityChange = -stockQuantity,
                            BalanceBefore = balanceBefore,
                            BalanceAfter = product.Quantity,
                            UnitCost = product.AverageCost,
                            SourceType = "SalesInvoice",
                            SourceId = invoice.Id.ToString(),
                            Reference = invoice.InvoiceNumber,
                            OccurredAtUtc = postedAtUtc,
                            CreatedBy = _currentUserService.Username
                        },
                        transactionToken);
                }

                invoice.Customer.BalanceDue += invoice.GrandTotal;
                invoice.Customer.UpdatedAtUtc = postedAtUtc;
                invoice.Status = SalesInvoiceStatus.Posted;
                invoice.PostedAtUtc = postedAtUtc;
                invoice.UpdatedAtUtc = postedAtUtc;
                await _invoiceRepository.SaveChangesAsync(transactionToken);
                created = invoice;
            }, cancellationToken);

            return created!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
