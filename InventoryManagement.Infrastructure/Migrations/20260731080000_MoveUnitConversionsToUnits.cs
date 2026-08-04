using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    public partial class MoveUnitConversionsToUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseUnitId",
                table: "Units",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactorToBaseUnit",
                table: "Units",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql(
                @"UPDATE ""Units""
                  SET ""BaseUnitId"" = ""Id"",
                      ""FactorToBaseUnit"" = 1
                  WHERE ""BaseUnitId"" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Units_BaseUnitId",
                table: "Units",
                column: "BaseUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Units_BaseUnitId",
                table: "Units",
                column: "BaseUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Units_BaseUnitId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_BaseUnitId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "BaseUnitId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "FactorToBaseUnit",
                table: "Units");
        }
    }
}
