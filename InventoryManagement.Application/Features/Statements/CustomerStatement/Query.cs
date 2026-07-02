using MediatR;

namespace InventoryManagement.Application.Features.Statements.CustomerStatement
{
    public class Query : IRequest<StatementResponse>
    {
        public int CustomerId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
