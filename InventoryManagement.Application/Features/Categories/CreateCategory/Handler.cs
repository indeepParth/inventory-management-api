using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;

namespace InventoryManagement.Application.Features.Categories.CreateCategory
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
            var name = request.Name.Trim();
            var existingCategory = await _repository.GetByNameAsync(name, cancellationToken);

            if (existingCategory is not null)
            {
                throw new BadRequestException("Category name already exists.");
            }

            var category = new Category
            {
                Name = name,
                Description = request.Description?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(category, cancellationToken);
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
