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
        Task<List<DeliveryChallan>> GetDriverDeliveriesAsync(
            int driverId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? isDeliveryChargePaid,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<int> GetDriverDeliveriesCountAsync(
            int driverId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? isDeliveryChargePaid,
            CancellationToken cancellationToken = default);
        Task<DeliveryChallan?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<DeliveryChallan?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<bool> ChallanNumberExistsAsync(
            string challanNumber,
            CancellationToken cancellationToken = default);
        Task<bool> ChallanNumberExistsForOtherAsync(
            string challanNumber,
            int deliveryChallanId,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            DeliveryChallan challan,
            CancellationToken cancellationToken = default);
        void RemoveItems(IEnumerable<DeliveryChallanItem> items);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
