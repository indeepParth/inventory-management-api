using MediatR;

namespace InventoryManagement.Application.Features.Drivers.UpdateDriver
{
    public sealed record Command(
        int Id,
        string Name,
        string? Phone,
        string? LicenseNumber,
        bool IsActive) : IRequest<DriverResponse>;
}
