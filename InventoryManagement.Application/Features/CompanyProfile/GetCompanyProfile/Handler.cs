using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.CompanyProfile.GetCompanyProfile
{
    public class Handler : IRequestHandler<Query, CompanyProfileResponse>
    {
        private readonly ICompanyProfileRepository _repository;

        public Handler(ICompanyProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<CompanyProfileResponse> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var profile = await _repository.GetAsync(cancellationToken);
            return profile.ToResponse();
        }
    }
}
