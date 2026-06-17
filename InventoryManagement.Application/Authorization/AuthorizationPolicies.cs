using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Application.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string CanDeleteProducts =
            nameof(CanDeleteProducts);

        public const string CanManageUsers =
            nameof(CanManageUsers);
    }
}