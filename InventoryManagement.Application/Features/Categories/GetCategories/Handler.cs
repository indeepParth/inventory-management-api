using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Categories.GetCategories
{
    public class Handler : IRequestHandler<Query, List<InventoryManagement.Application.Features.Categories.Response>>
    {
        private readonly ICategoryRepository _repository;

        public Handler(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<InventoryManagement.Application.Features.Categories.Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var categories = await _repository.GetAllAsync(cancellationToken);

            return categories
                .Select(category => new InventoryManagement.Application.Features.Categories.Response
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt
                })
                .ToList();
        }
    }
}
