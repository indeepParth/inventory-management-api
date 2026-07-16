using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDriverDeliveries
{
    public class Query : IRequest<DriverDeliveriesResponse>
    {
        public int DriverId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DriverDeliveryPaymentStatus PaymentStatus { get; set; } =
            DriverDeliveryPaymentStatus.All;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
