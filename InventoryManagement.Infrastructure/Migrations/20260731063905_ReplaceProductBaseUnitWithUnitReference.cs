using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProductBaseUnitWithUnitReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BaseUnit",
                table: "Products",
                newName: "BaseUnitId");

            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "BaseUnitId" = 4
                WHERE "BaseUnitId" NOT IN (1, 2, 3, 4, 5, 6);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BaseUnitId",
                table: "Products",
                column: "BaseUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_BaseUnitId",
                table: "Products",
                column: "BaseUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_BaseUnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_BaseUnitId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "BaseUnitId",
                table: "Products",
                newName: "BaseUnit");
        }
    }
}
