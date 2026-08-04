using MediatR;

namespace InventoryManagement.Application.Features.Units.GetUnitById
{
    public class Query : IRequest<InventoryManagement.Application.Features.Units.Response>
    {
        public int Id { get; set; }
    }
}
