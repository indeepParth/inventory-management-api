using InventoryManagement.Application.Common.Interfaces;

namespace InventoryManagement.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Username =>
            _httpContextAccessor.HttpContext?
            .User.Identity?.Name ?? "";
    }
}
