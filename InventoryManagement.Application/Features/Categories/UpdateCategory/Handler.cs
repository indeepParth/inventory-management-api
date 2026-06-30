using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Categories.UpdateCategory
{
    public class Handler : IRequestHandler<Command, InventoryManagement.Application.Features.Categories.Response>
    {
        private readonly ICategoryRepository _repository;

        public Handler(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<InventoryManagement.Application.Features.Categories.Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (category is null)
            {
                throw new NotFoundException("Category not found.");
            }

            var name = request.Name.Trim();
            var existingCategory = await _repository.GetByNameAsync(name, cancellationToken);

            if (existingCategory is not null && existingCategory.Id != request.Id)
            {
                throw new BadRequestException("Category name already exists.");
            }

            category.Name = name;
            category.Description = request.Description?.Trim() ?? string.Empty;
            category.IsActive = request.IsActive;

            await _repository.SaveChangesAsync(cancellationToken);

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
