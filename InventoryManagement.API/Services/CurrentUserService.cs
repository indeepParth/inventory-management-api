using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public bool IsAdmin =>
            Roles.Contains("Admin");

        public IEnumerable<string> Roles =>
            _httpContextAccessor.HttpContext?
            .User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            ?? Enumerable.Empty<string>();
    }
}