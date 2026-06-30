using MediatR;

namespace InventoryManagement.Application.Features.Categories.GetCategoryById
{
    public class Query : IRequest<InventoryManagement.Application.Features.Categories.Response>
    {
        public int Id { get; set; }
    }
}
