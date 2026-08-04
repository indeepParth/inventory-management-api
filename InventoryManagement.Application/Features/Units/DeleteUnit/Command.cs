using MediatR;

namespace InventoryManagement.Application.Features.Units.DeleteUnit
{
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
    }
}
