using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.CreateSupplier
{
    public class Command : IRequest<SupplierResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? GstNumber { get; set; }
    }
}
