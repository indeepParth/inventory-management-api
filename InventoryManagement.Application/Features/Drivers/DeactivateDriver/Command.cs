using MediatR;

namespace InventoryManagement.Application.Features.Drivers.DeactivateDriver
{
    public class Command : IRequest<DriverResponse>
    {
        public int Id { get; set; }
    }
}
