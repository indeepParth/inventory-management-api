using MediatR;

namespace InventoryManagement.Application.Features.Categories.UpdateCategory
{
    public sealed record Command(
        int Id,
        string Name,
        string Description,
        bool IsActive
    ) : IRequest<InventoryManagement.Application.Features.Categories.Response>;
}
