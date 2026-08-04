using MediatR;

namespace InventoryManagement.Application.Features.Units.GetUnits
{
    public class Query : IRequest<List<InventoryManagement.Application.Features.Units.Response>>
    {
    }
}
