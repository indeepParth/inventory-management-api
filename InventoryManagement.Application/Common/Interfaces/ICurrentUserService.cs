

namespace InventoryManagement.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string Username { get; }
        bool IsAdmin { get; }
        IEnumerable<string> Roles { get; }
    }
}