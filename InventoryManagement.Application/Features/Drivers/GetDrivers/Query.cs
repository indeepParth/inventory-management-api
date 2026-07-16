using InventoryManagement.Application.Common.Models;
using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDrivers
{
    public class Query : IRequest<PagedResponse<DriverResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
