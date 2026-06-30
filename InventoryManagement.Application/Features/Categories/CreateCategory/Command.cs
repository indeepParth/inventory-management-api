using MediatR;

namespace InventoryManagement.Application.Features.Categories.CreateCategory
{
    public class Command : IRequest<InventoryManagement.Application.Features.Categories.Response>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
