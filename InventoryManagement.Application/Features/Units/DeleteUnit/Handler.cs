using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Units.DeleteUnit
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IUnitRepository _repository;

        public Handler(IUnitRepository repository)
        {
            _repository = repository;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var unit = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (unit is null)
            {
                throw new NotFoundException("Unit not found.");
            }

            await _repository.DeleteAsync(unit, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new Response
            {
                Id = request.Id,
                Message = "Unit deleted successfully."
            };
        }
    }
}
