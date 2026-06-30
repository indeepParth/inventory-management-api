using MediatR;

namespace InventoryManagement.Application.Features.Categories.GetCategories
{
    public class Query : IRequest<List<InventoryManagement.Application.Features.Categories.Response>>
    {
    }
}
