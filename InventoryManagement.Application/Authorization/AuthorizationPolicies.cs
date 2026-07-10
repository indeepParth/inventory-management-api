using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Application.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string AdminOrManager = nameof(AdminOrManager);

        public const string ReadProducts = nameof(ReadProducts);
        public const string ManageProducts = nameof(ManageProducts);

        public const string ReadCustomers = nameof(ReadCustomers);
        public const string ManageCustomers = nameof(ManageCustomers);
        public const string ViewCustomerStatements = nameof(ViewCustomerStatements);

        public const string ReadSuppliers = nameof(ReadSuppliers);
        public const string ManageSuppliers = nameof(ManageSuppliers);
        public const string ViewSupplierStatements = nameof(ViewSupplierStatements);

        public const string ManagePurchases = nameof(ManagePurchases);
        public const string ManageDeliveryChallans = nameof(ManageDeliveryChallans);
        public const string ManageSalesInvoices = nameof(ManageSalesInvoices);
        public const string ManageSupplierReturns = nameof(ManageSupplierReturns);
        public const string ManageCustomerReturns = nameof(ManageCustomerReturns);

        public const string ViewPayments = nameof(ViewPayments);
        public const string CreateCustomerReceipts = nameof(CreateCustomerReceipts);

        public const string ViewStockMovements = nameof(ViewStockMovements);
        public const string RecordStockDamage = nameof(RecordStockDamage);

        public const string ViewProductStockLedger = nameof(ViewProductStockLedger);
        public const string ViewSalesReports = nameof(ViewSalesReports);
        public const string ViewCostReports = nameof(ViewCostReports);
    }
}
