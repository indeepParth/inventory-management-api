using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IDeliveryChallanRepository
    {
        Task<List<DeliveryChallan>> GetAllAsync(
            int pageNumber,
            int pageSize,
            int? customerId,
            DeliveryChallanStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? challanNumber,
            CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            int? customerId,
            DeliveryChallanStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? challanNumber,
            CancellationToken cancellationToken = default);
        Task<DeliveryChallan?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<bool> ChallanNumberExistsAsync(
            string challanNumber,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            DeliveryChallan challan,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
