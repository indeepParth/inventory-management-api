using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;

public class Handler : IRequestHandler<Query, RegisterResponse<Response>>
{
    private readonly ISalesInvoiceRepository _repository;

    public Handler(ISalesInvoiceRepository repository) => _repository = repository;

    public async Task<RegisterResponse<Response>> Handle(
        Query request,
        CancellationToken cancellationToken)
    {
        var source = request.SourceType.HasValue
            ? request.SourceType == SalesSourceType.DeliveryChallan
            : (bool?)null;
        var invoices = await _repository.GetRegisterAsync(
            request.PageNumber, request.PageSize, request.CustomerId,
            request.ProductId, source, request.Status, request.FromDate,
            request.ToDate, cancellationToken);
        var totalCount = await _repository.GetRegisterCountAsync(
            request.CustomerId, request.ProductId, source, request.Status,
            request.FromDate, request.ToDate, cancellationToken);
        var summaryDocuments = await _repository.GetRegisterSummaryAsync(
            request.CustomerId, request.ProductId, source, request.Status,
            request.FromDate, request.ToDate, cancellationToken);

        if (!request.IncludeDraftAndCancelledInSummary)
        {
            summaryDocuments = summaryDocuments
                .Where(x => x.Status != SalesInvoiceStatus.Draft &&
                            x.Status != SalesInvoiceStatus.Cancelled)
                .ToList();
        }

        return new RegisterResponse<Response>
        {
            Items = invoices.Select(Map).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Summary = BuildSummary(summaryDocuments)
        };
    }

    private static Response Map(SalesInvoice invoice) => new()
    {
        SalesInvoiceId = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Date = invoice.InvoiceDate,
        CustomerId = invoice.CustomerId,
        CustomerName = invoice.Customer.Name,
        SourceType = invoice.Items.Any(x => x.DeliveryChallanItemId.HasValue)
            ? SalesSourceType.DeliveryChallan
            : SalesSourceType.Direct,
        Status = invoice.Status,
        TotalQuantity = invoice.Items.Sum(x => x.Quantity),
        Subtotal = invoice.Subtotal,
        Discount = invoice.Discount,
        TaxAmount = invoice.TaxAmount,
        OtherCharges = invoice.OtherCharges,
        GrandTotal = invoice.GrandTotal,
        PaidAmount = invoice.AmountPaid,
        OutstandingAmount = invoice.BalanceDue,
        Products = invoice.Items.Select(x => new ProductLine
        {
            ProductId = x.ProductId,
            ProductName = x.Product.Name,
            Quantity = x.Quantity,
            TaxAmount = x.TaxAmount,
            LineTotal = x.LineTotal
        }).ToList()
    };

    private static RegisterSummary BuildSummary(IReadOnlyCollection<SalesInvoice> documents) => new()
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
