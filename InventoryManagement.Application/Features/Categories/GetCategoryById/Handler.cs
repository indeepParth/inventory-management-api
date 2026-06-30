using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Categories.GetCategoryById
{
    public class Handler : IRequestHandler<Query, InventoryManagement.Application.Features.Categories.Response>
    {
        private readonly ICategoryRepository _repository;

        public Handler(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<InventoryManagement.Application.Features.Categories.Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var category = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            return new InventoryManagement.Application.Features.Categories.Response
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }
    }
}
