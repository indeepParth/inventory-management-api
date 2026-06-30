using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitsAndDecimalInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "DefaultSellingPrice");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BaseUnit",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultSellingPrice",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageCost",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseUnit",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultSellingPrice",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.RenameColumn(
                name: "DefaultSellingPrice",
                table: "Products",
                newName: "Price");
        }
    }
}
