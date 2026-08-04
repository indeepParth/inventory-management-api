using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryChallanItemUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "DeliveryChallanItems",
                newName: "EnteredQuantity");

            migrationBuilder.AddColumn<decimal>(
                name: "ConvertedBaseQuantity",
                table: "DeliveryChallanItems",
                type: "TEXT",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "DeliveryChallanItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "DeliveryChallanItems"
                SET "ConvertedBaseQuantity" = "EnteredQuantity";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "DeliveryChallanItems"
                SET "UnitId" = COALESCE((
                    SELECT "Products"."BaseUnitId"
                    FROM "Products"
                    WHERE "Products"."Id" = "DeliveryChallanItems"."ProductId"
                ), 4);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_UnitId",
                table: "DeliveryChallanItems",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallanItems_Units_UnitId",
                table: "DeliveryChallanItems",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallanItems_Units_UnitId",
                table: "DeliveryChallanItems");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallanItems_UnitId",
                table: "DeliveryChallanItems");

            migrationBuilder.DropColumn(
                name: "ConvertedBaseQuantity",
                table: "DeliveryChallanItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "DeliveryChallanItems");

            migrationBuilder.RenameColumn(
                name: "EnteredQuantity",
                table: "DeliveryChallanItems",
                newName: "Quantity");
        }
    }
}
