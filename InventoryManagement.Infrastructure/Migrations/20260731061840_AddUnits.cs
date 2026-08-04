using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Ton", null },
                    { 2, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Kilogram", null },
                    { 3, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Bag", null },
                    { 4, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Piece", null },
                    { 5, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Cubic foot", null },
                    { 6, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, "Cubic meter", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Units");
        }
    }
}
