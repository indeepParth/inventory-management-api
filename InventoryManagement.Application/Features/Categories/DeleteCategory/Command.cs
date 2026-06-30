using MediatR;

namespace InventoryManagement.Application.Features.Categories.DeleteCategory
{
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
    }
}
