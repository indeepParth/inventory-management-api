using MediatR;

namespace InventoryManagement.Application.Features.Drivers.GetDriverById
{
    public class Query : IRequest<DriverResponse>
    {
        public int Id { get; set; }
    }
}
