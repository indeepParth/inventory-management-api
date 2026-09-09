namespace InventoryManagement.Application.Common.Interfaces
{
    public interface ISubscriptionLimitService
    {
        Task EnsureCanCreateCompanyAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task EnsureCanAddCompanyMemberAsync(
            int companyId,
            CancellationToken cancellationToken = default);

        Task EnsureCanInviteCompanyMemberAsync(
            int companyId,
            CancellationToken cancellationToken = default);

        Task EnsureCanCreateProductAsync(
            CancellationToken cancellationToken = default);

        Task EnsureCanCreateCustomerAsync(
            CancellationToken cancellationToken = default);

        Task EnsureCanCreateSalesInvoiceAsync(
            CancellationToken cancellationToken = default);
    }
}
