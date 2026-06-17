using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetByTokenAsync(
            string token,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}