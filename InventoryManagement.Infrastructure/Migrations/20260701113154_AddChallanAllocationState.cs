using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallanAllocationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.AddColumn<bool>(
                name: "IsChallanAllocationActive",
                table: "SalesInvoiceItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE SalesInvoiceItems
                SET IsChallanAllocationActive = 1
                WHERE DeliveryChallanItemId IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems",
                column: "DeliveryChallanItemId",
                unique: true,
                filter: "\"DeliveryChallanItemId\" IS NOT NULL AND \"IsChallanAllocationActive\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsChallanAllocationActive",
                table: "SalesInvoiceItems");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems",
                column: "DeliveryChallanItemId",
                unique: true,
                filter: "\"DeliveryChallanItemId\" IS NOT NULL");
        }
    }
}
