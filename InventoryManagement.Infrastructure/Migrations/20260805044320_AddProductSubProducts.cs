using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSubProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseProductId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactorToBaseProduct",
                table: "Products",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BaseProductId",
                table: "Products",
                column: "BaseProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Products_BaseProductId",
                table: "Products",
                column: "BaseProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Products_BaseProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_BaseProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FactorToBaseProduct",
                table: "Products");

        }
    }
}
