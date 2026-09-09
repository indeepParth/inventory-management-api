namespace InventoryManagement.Application.Authorization
{
    public static class CompanyRoles
    {
        public const string Owner = nameof(Owner);
        public const string Admin = nameof(Admin);
        public const string Manager = nameof(Manager);
        public const string Staff = nameof(Staff);
        public const string Viewer = nameof(Viewer);

        public static readonly string[] All =
        [
            Owner,
            Admin,
            Manager,
            Staff,
            Viewer
        ];
    }
}
