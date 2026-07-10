namespace InventoryManagement.Application.Authorization
{
    public static class ApplicationRoles
    {
        public const string Admin = nameof(Admin);
        public const string Manager = nameof(Manager);
        public const string Sales = nameof(Sales);
        public const string Inventory = nameof(Inventory);

        public static readonly string[] All =
        [
            Admin,
            Manager,
            Sales,
            Inventory
        ];
    }
}
