using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesInvoiceDriverCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "SalesInvoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "SalesInvoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeliveryChargePaid",
                table: "SalesInvoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_DriverId",
                table: "SalesInvoices",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Drivers_DriverId",
                table: "SalesInvoices",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Drivers_DriverId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_DriverId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "IsDeliveryChargePaid",
                table: "SalesInvoices");
        }
    }
}
