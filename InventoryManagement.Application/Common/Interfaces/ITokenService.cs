

namespace InventoryManagement.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(
            string userId,
            string userName,
            IEnumerable<string> roles);

        string GenerateRefreshToken();
    }
}