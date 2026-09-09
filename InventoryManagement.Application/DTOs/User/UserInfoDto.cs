using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Application.DTOs.User
{
    public class UserInfoDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsDisabled { get; set; }
        public List<UserCompanyDto> Companies { get; set; } = new();
    }
}
