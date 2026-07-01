using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateChallanItemInvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems",
                column: "DeliveryChallanItemId",
                unique: true,
                filter: "\"DeliveryChallanItemId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceItems_DeliveryChallanItemId",
                table: "SalesInvoiceItems",
                column: "DeliveryChallanItemId");
        }
    }
}
