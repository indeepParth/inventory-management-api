namespace InventoryManagement.Application.Authorization
{
    public static class CompanyPermissions
    {
        private static readonly IReadOnlyDictionary<string, string[]> RolePermissions =
            new Dictionary<string, string[]>
            {
                [CompanyRoles.Owner] = AllPermissions(),
                [CompanyRoles.Admin] = AllPermissions(),
                [CompanyRoles.Manager] =
                [
                    AuthorizationPolicies.AdminOrManager,
                    AuthorizationPolicies.ReadProducts,
                    AuthorizationPolicies.ManageProducts,
                    AuthorizationPolicies.ReadCustomers,
                    AuthorizationPolicies.ManageCustomers,
                    AuthorizationPolicies.ViewCustomerStatements,
                    AuthorizationPolicies.ReadDrivers,
                    AuthorizationPolicies.ManageDrivers,
                    AuthorizationPolicies.ReadSuppliers,
                    AuthorizationPolicies.ManageSuppliers,
                    AuthorizationPolicies.ViewSupplierStatements,
                    AuthorizationPolicies.ManagePurchases,
                    AuthorizationPolicies.ManageDeliveryChallans,
                    AuthorizationPolicies.ManageSalesInvoices,
                    AuthorizationPolicies.ManageSupplierReturns,
                    AuthorizationPolicies.ManageCustomerReturns,
                    AuthorizationPolicies.ViewPayments,
                    AuthorizationPolicies.CreateCustomerReceipts,
                    AuthorizationPolicies.ViewStockMovements,
                    AuthorizationPolicies.RecordStockDamage,
                    AuthorizationPolicies.ViewProductStockLedger,
                    AuthorizationPolicies.ViewSalesReports,
                    AuthorizationPolicies.ViewCostReports
                ],
                [CompanyRoles.Staff] =
                [
                    AuthorizationPolicies.ReadProducts,
                    AuthorizationPolicies.ReadCustomers,
                    AuthorizationPolicies.ViewCustomerStatements,
                    AuthorizationPolicies.ReadDrivers,
                    AuthorizationPolicies.ReadSuppliers,
                    AuthorizationPolicies.ViewSupplierStatements,
                    AuthorizationPolicies.ManagePurchases,
                    AuthorizationPolicies.ManageDeliveryChallans,
                    AuthorizationPolicies.ManageSalesInvoices,
                    AuthorizationPolicies.ManageSupplierReturns,
                    AuthorizationPolicies.ManageCustomerReturns,
                    AuthorizationPolicies.ViewPayments,
                    AuthorizationPolicies.CreateCustomerReceipts,
                    AuthorizationPolicies.ViewStockMovements,
                    AuthorizationPolicies.RecordStockDamage,
                    AuthorizationPolicies.ViewProductStockLedger,
                    AuthorizationPolicies.ViewSalesReports
                ],
                [CompanyRoles.Viewer] =
                [
                    AuthorizationPolicies.ReadProducts,
                    AuthorizationPolicies.ReadCustomers,
                    AuthorizationPolicies.ViewCustomerStatements,
                    AuthorizationPolicies.ReadDrivers,
                    AuthorizationPolicies.ReadSuppliers,
                    AuthorizationPolicies.ViewSupplierStatements,
                    AuthorizationPolicies.ViewPayments,
                    AuthorizationPolicies.ViewStockMovements,
                    AuthorizationPolicies.ViewProductStockLedger,
                    AuthorizationPolicies.ViewSalesReports,
                    AuthorizationPolicies.ViewCostReports
                ]
            };

        public static IReadOnlyList<string> GetPermissionsForRole(string role)
        {
            return RolePermissions.TryGetValue(role, out var permissions)
                ? permissions
                : [];
        }

        public static string[] GetRolesForPermission(string permission)
        {
            return RolePermissions
                .Where(x => x.Value.Contains(permission))
                .Select(x => x.Key)
                .ToArray();
        }

        private static string[] AllPermissions()
        {
            return
            [
                AuthorizationPolicies.AdminOnly,
                AuthorizationPolicies.AdminOrManager,
                AuthorizationPolicies.ReadProducts,
                AuthorizationPolicies.ManageProducts,
                AuthorizationPolicies.ReadCustomers,
                AuthorizationPolicies.ManageCustomers,
                AuthorizationPolicies.ViewCustomerStatements,
                AuthorizationPolicies.ReadDrivers,
                AuthorizationPolicies.ManageDrivers,
                AuthorizationPolicies.ReadSuppliers,
                AuthorizationPolicies.ManageSuppliers,
                AuthorizationPolicies.ViewSupplierStatements,
                AuthorizationPolicies.ManagePurchases,
                AuthorizationPolicies.ManageDeliveryChallans,
                AuthorizationPolicies.ManageSalesInvoices,
                AuthorizationPolicies.ManageSupplierReturns,
                AuthorizationPolicies.ManageCustomerReturns,
                AuthorizationPolicies.ViewPayments,
                AuthorizationPolicies.CreateCustomerReceipts,
                AuthorizationPolicies.ViewStockMovements,
                AuthorizationPolicies.RecordStockDamage,
                AuthorizationPolicies.ViewProductStockLedger,
                AuthorizationPolicies.ViewSalesReports,
                AuthorizationPolicies.ViewCostReports
            ];
        }
    }
}
