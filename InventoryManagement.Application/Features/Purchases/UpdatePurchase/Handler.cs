using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.UpdatePurchase
{
    public class Handler : IRequestHandler<Command, PurchaseResponse>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IProductRepository _productRepository;

        public Handler(
            IPurchaseRepository purchaseRepository,
            ISupplierRepository supplierRepository,
            IProductRepository productRepository)
        {
            _purchaseRepository = purchaseRepository;
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
        }

        public async Task<PurchaseResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var purchase = await _purchaseRepository.GetForUpdateAsync(
                request.Id,
                cancellationToken) ?? throw new NotFoundException("Purchase not found.");

            if (purchase.Status != PurchaseStatus.Draft)
            {
                throw new BadRequestException("Only Draft purchases may be edited.");
            }

            var supplierBillNumber = NormalizeOptional(request.SupplierBillNumber);

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
                await _purchaseRepository.SupplierBillNumberExistsForOtherAsync(
                    supplier.Id,
                    supplierBillNumber,
                    purchase.Id,
                    cancellationToken))
            {
                throw new BadRequestException(
                    "Supplier bill number already exists for this supplier.");
            }

            var replacementItems = new List<PurchaseItem>();
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

                var lineSubtotal = RoundMoney(input.Quantity * input.UnitCost);
                var taxAmount = RoundMoney(lineSubtotal * input.TaxRate / 100m);
                replacementItems.Add(new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = input.Quantity,
                    UnitCost = input.UnitCost,
                    TaxRate = input.TaxRate,
                    TaxAmount = taxAmount,
                    LineTotal = lineSubtotal + taxAmount
                });
                subtotal += lineSubtotal;
                taxAmountTotal += taxAmount;
            }

            purchase.SupplierId = supplier.Id;
            purchase.Supplier = supplier;
            purchase.SupplierBillNumber = supplierBillNumber;
            purchase.BillDate = request.BillDate;
            purchase.Discount = request.Discount;
            purchase.OtherCharges = request.OtherCharges;
            purchase.Notes = NormalizeOptional(request.Notes);
            purchase.Subtotal = subtotal;
            purchase.TaxAmount = taxAmountTotal;
            purchase.GrandTotal = RoundMoney(
                subtotal - request.Discount + taxAmountTotal + request.OtherCharges);
            purchase.AmountPaid = 0;
            purchase.BalanceDue = purchase.GrandTotal;
            _purchaseRepository.RemoveItems(purchase.Items);
            purchase.Items.Clear();

            foreach (var item in replacementItems)
            {
                purchase.Items.Add(item);
            }

            await _purchaseRepository.SaveChangesAsync(cancellationToken);
            return purchase.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
