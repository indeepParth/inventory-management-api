namespace InventoryManagement.Domain.Enums
{
    public enum StockMovementType
    {
        OpeningStock = 0,
        Purchase = 1,
        Sale = 2,
        CustomerReturn = 3,
        SupplierReturn = 4,
        Adjustment = 5,
        Damage = 6,
        Reversal = 7
    }
}
