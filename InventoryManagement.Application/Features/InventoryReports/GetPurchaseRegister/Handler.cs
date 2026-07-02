using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;

public class Handler : IRequestHandler<Query, RegisterResponse<Response>>
{
    private readonly IPurchaseRepository _repository;

    public Handler(IPurchaseRepository repository) => _repository = repository;

    public async Task<RegisterResponse<Response>> Handle(
        Query request,
        CancellationToken cancellationToken)
    {
        var purchases = await _repository.GetRegisterAsync(
            request.PageNumber, request.PageSize, request.SupplierId,
            request.ProductId, request.Status, request.FromDate, request.ToDate,
            cancellationToken);
        var totalCount = await _repository.GetRegisterCountAsync(
            request.SupplierId, request.ProductId, request.Status,
            request.FromDate, request.ToDate, cancellationToken);
        var summaryDocuments = await _repository.GetRegisterSummaryAsync(
            request.SupplierId, request.ProductId, request.Status,
            request.FromDate, request.ToDate, cancellationToken);

        if (!request.IncludeDraftAndCancelledInSummary)
        {
            summaryDocuments = summaryDocuments
                .Where(x => x.Status != PurchaseStatus.Draft &&
                            x.Status != PurchaseStatus.Cancelled)
                .ToList();
        }

        return new RegisterResponse<Response>
        {
            Items = purchases.Select(Map).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Summary = BuildSummary(summaryDocuments)
        };
    }

    private static Response Map(Purchase purchase) => new()
    {
        PurchaseId = purchase.Id,
        PurchaseNumber = purchase.PurchaseNumber,
        Date = purchase.BillDate,
        SupplierId = purchase.SupplierId,
        SupplierName = purchase.Supplier.Name,
        Status = purchase.Status,
        TotalQuantity = purchase.Items.Sum(x => x.Quantity),
        Subtotal = purchase.Subtotal,
        Discount = purchase.Discount,
        TaxAmount = purchase.TaxAmount,
        OtherCharges = purchase.OtherCharges,
        GrandTotal = purchase.GrandTotal,
        PaidAmount = purchase.AmountPaid,
        OutstandingAmount = purchase.BalanceDue,
        Products = purchase.Items.Select(x => new ProductLine
        {
            ProductId = x.ProductId,
            ProductName = x.Product.Name,
            Quantity = x.Quantity,
            TaxAmount = x.TaxAmount,
            LineTotal = x.LineTotal
        }).ToList()
    };

    private static RegisterSummary BuildSummary(IReadOnlyCollection<Purchase> documents) => new()
    {
        DocumentCount = documents.Count,
        TotalQuantity = documents.SelectMany(x => x.Items).Sum(x => x.Quantity),
        Subtotal = documents.Sum(x => x.Subtotal),
        Discount = documents.Sum(x => x.Discount),
        TaxAmount = documents.Sum(x => x.TaxAmount),
        OtherCharges = documents.Sum(x => x.OtherCharges),
        GrandTotal = documents.Sum(x => x.GrandTotal),
        PaidAmount = documents.Sum(x => x.AmountPaid),
        OutstandingAmount = documents.Sum(x => x.BalanceDue)
    };
}
