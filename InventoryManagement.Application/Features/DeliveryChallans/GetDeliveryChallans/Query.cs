using InventoryManagement.Application.Common.Models;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallans
{
    public class Query : IRequest<PagedResponse<DeliveryChallanResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CustomerId { get; set; }
        public DeliveryChallanStatus? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? ChallanNumber { get; set; }
    }
}
