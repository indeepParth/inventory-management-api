using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryChallanDriverAndCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCharge",
                table: "DeliveryChallans",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFromAddress",
                table: "DeliveryChallans",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "DeliveryChallans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeliveryChargePaid",
                table: "DeliveryChallans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_DriverId",
                table: "DeliveryChallans",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallans_Drivers_DriverId",
                table: "DeliveryChallans",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallans_Drivers_DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "DeliveryCharge",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "DeliveryFromAddress",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "IsDeliveryChargePaid",
                table: "DeliveryChallans");
        }
    }
}
