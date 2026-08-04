using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Units.GetUnitById
{
    public class Handler : IRequestHandler<Query, InventoryManagement.Application.Features.Units.Response>
    {
        private readonly IUnitRepository _repository;

        public Handler(IUnitRepository repository)
        {
            _repository = repository;
        }

        public async Task<InventoryManagement.Application.Features.Units.Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var unit = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (unit is null)
            {
                throw new NotFoundException("Unit not found.");
            }

            return unit.ToResponse();
        }
    }
}
