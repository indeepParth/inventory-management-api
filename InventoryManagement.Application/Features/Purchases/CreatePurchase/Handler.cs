using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.CreatePurchase
{
    public class Handler : IRequestHandler<Command, PurchaseResponse>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;

        public Handler(
            IPurchaseRepository purchaseRepository,
            ISupplierRepository supplierRepository,
            IProductRepository productRepository,
            ICurrentUserService currentUserService)
        {
            _purchaseRepository = purchaseRepository;
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PurchaseResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var purchaseNumber = request.PurchaseNumber.Trim();
            var supplierBillNumber = NormalizeOptional(request.SupplierBillNumber);

            if (await _purchaseRepository.PurchaseNumberExistsAsync(
                purchaseNumber,
                cancellationToken))
            {
                throw new BadRequestException("Purchase number already exists.");
            }

            var supplier = await _supplierRepository.GetByIdAsync(
                request.SupplierId,
                cancellationToken);

            if (supplier is null)
            {
                throw new NotFoundException("Supplier not found.");
            }

            if (!supplier.IsActive)
            {
                throw new BadRequestException("Supplier is inactive.");
            }

            if (supplierBillNumber is not null &&
                await _purchaseRepository.SupplierBillNumberExistsAsync(
                    supplier.Id,
                    supplierBillNumber,
                    cancellationToken))
            {
                throw new BadRequestException(
                    "Supplier bill number already exists for this supplier.");
            }

            var purchase = new Purchase
            {
                PurchaseNumber = purchaseNumber,
                SupplierId = supplier.Id,
                Supplier = supplier,
                SupplierBillNumber = supplierBillNumber,
                BillDate = request.BillDate,
                Status = PurchaseStatus.Draft,
                Discount = request.Discount,
                OtherCharges = request.OtherCharges,
                Notes = NormalizeOptional(request.Notes),
                CreatedAtUtc = DateTime.UtcNow,
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

                var lineSubtotal = RoundMoney(input.Quantity * input.UnitCost);
                var taxAmount = RoundMoney(lineSubtotal * input.TaxRate / 100m);

                purchase.Items.Add(new PurchaseItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity,
                    UnitCost = input.UnitCost,
                    TaxRate = input.TaxRate,
                    TaxAmount = taxAmount,
                    LineTotal = lineSubtotal + taxAmount
                });

                purchase.Subtotal += lineSubtotal;
                purchase.TaxAmount += taxAmount;
            }

            purchase.GrandTotal = RoundMoney(
                purchase.Subtotal -
                purchase.Discount +
                purchase.TaxAmount +
                purchase.OtherCharges);

            await _purchaseRepository.AddAsync(purchase, cancellationToken);
            await _purchaseRepository.SaveChangesAsync(cancellationToken);

            return purchase.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
