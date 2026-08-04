using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Units.GetUnits
{
    public class Handler : IRequestHandler<Query, List<InventoryManagement.Application.Features.Units.Response>>
    {
        private readonly IUnitRepository _repository;

        public Handler(IUnitRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<InventoryManagement.Application.Features.Units.Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var units = await _repository.GetAllAsync(cancellationToken);

            return units.Select(unit => unit.ToResponse()).ToList();
        }
    }
}
