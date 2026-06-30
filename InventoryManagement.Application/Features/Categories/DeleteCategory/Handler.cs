using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Categories.DeleteCategory
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly ICategoryRepository _repository;

        public Handler(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            if (await _repository.HasProductsAsync(request.Id, cancellationToken))
            {
                throw new BadRequestException("Category cannot be deleted because it has products.");
            }

            await _repository.DeleteAsync(category, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Response
            {
                Id = request.Id,
                Message = "Category deleted successfully."
            };
        }
    }
}
