using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Products
{
    internal static class ProductStock
    {
        public static bool IsSubProduct(Product product) =>
            product.BaseProductId.HasValue;

        public static decimal GetAvailableQuantity(Product product)
        {
            if (!IsSubProduct(product))
            {
                return product.Quantity;
            }

            if (product.BaseProduct is null ||
                !product.FactorToBaseProduct.HasValue ||
                product.FactorToBaseProduct.Value <= 0m)
            {
                return 0m;
            }

            return product.BaseProduct.Quantity / product.FactorToBaseProduct.Value;
        }

        public static Product GetStockProduct(Product product) =>
            product.BaseProduct ?? product;

        public static decimal GetStockQuantity(Product product, decimal quantity) =>
            IsSubProduct(product)
                ? quantity * product.FactorToBaseProduct!.Value
                : quantity;

        public static decimal GetCostAtSale(Product product)
        {
            var stockProduct = GetStockProduct(product);
            var factor = IsSubProduct(product)
                ? product.FactorToBaseProduct!.Value
                : 1m;

            return stockProduct.AverageCost * factor;
        }
    }
}
