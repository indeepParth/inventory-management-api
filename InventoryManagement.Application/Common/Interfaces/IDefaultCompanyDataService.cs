namespace InventoryManagement.Application.Common.Interfaces
{
    public interface IDefaultCompanyDataService
    {
        Task SeedDefaultUnitsAsync(int companyId, CancellationToken cancellationToken = default);
    }
}
