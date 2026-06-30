using InventoryManagement.Application.Common.Models;
using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.GetSuppliers
{
    public class Query : IRequest<PagedResponse<SupplierResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
