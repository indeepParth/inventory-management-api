using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.CompanyProfile.UpdateCompanyProfile
{
    public class Handler : IRequestHandler<Command, CompanyProfileResponse>
    {
        private readonly ICompanyProfileRepository _repository;

        public Handler(ICompanyProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<CompanyProfileResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var profile = CompanyProfileMapping.ToEntity(
                request.CompanyName,
                request.Address,
                request.GstNumber,
                request.Email,
                request.Phone,
                request.Website);

            await _repository.UpsertAsync(profile, cancellationToken);

            return (await _repository.GetAsync(cancellationToken)).ToResponse();
        }
    }
}
