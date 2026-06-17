using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Application.DTOs.User
{
    public class UserInfoDto
    {
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}