namespace InventoryManagement.Application.Authorization
{
    public static class CompanyRoles
    {
        public const string Owner = nameof(Owner);
        public const string Manager = nameof(Manager);
        public const string Sales = nameof(Sales);
        public const string Inventory = nameof(Inventory);

        public static readonly string[] All =
        [
            Owner,
            Manager,
            Sales,
            Inventory
        ];
    }
}
