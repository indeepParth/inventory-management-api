using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.UpdateSupplier
{
    public sealed record Command(
        int Id,
        string Name,
        string? ContactPerson,
        string? Email,
        string? Phone,
        string? Address,
        string? GstNumber,
        bool IsActive) : IRequest<SupplierResponse>;
}
