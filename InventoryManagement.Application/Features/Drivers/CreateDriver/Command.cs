using MediatR;

namespace InventoryManagement.Application.Features.Drivers.CreateDriver
{
    public class Command : IRequest<DriverResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LicenseNumber { get; set; }
    }
}
