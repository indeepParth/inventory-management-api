using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.CreateCustomerReturn
{
    public class Command : IRequest<CustomerReturnResponse>
    {
        public string ReturnNumber { get; set; } = string.Empty;
        public int SalesInvoiceId { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? Notes { get; set; }
        public List<CustomerReturnItemInput> Items { get; set; } = new();
    }

    public class CustomerReturnItemInput
    {
        public int SalesInvoiceItemId { get; set; }
        public decimal Quantity { get; set; }
    }
}
