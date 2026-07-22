using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ICompanyProfileRepository
    {
        Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken = default);
        Task UpsertAsync(CompanyProfile companyProfile, CancellationToken cancellationToken = default);
    }
}
