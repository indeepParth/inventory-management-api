using System;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260627000000_AddCategories")]
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Description", "IsActive", "CreatedAt" },
                columnTypes: new[] { "INTEGER", "TEXT", "TEXT", "INTEGER", "TEXT" },
                values: new object[] { 1, "Uncategorized", "Default category for existing products.", true, new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TABLE "Products_WithCategories" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "SKU" TEXT NOT NULL,
                    "Quantity" INTEGER NOT NULL,
                    "Price" TEXT NOT NULL,
                    "CategoryId" INTEGER NOT NULL DEFAULT 1,
                    CONSTRAINT "FK_Products_Categories_CategoryId"
                        FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT
                );

                INSERT INTO "Products_WithCategories" ("Id", "Name", "SKU", "Quantity", "Price", "CategoryId")
                SELECT "Id", "Name", "SKU", "Quantity", "Price", 1
                FROM "Products";

                DROP TABLE "Products";
                ALTER TABLE "Products_WithCategories" RENAME TO "Products";
                CREATE INDEX "IX_Products_CategoryId" ON "Products" ("CategoryId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE "Products_WithoutCategories" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "SKU" TEXT NOT NULL,
                    "Quantity" INTEGER NOT NULL,
                    "Price" TEXT NOT NULL
                );

                INSERT INTO "Products_WithoutCategories" ("Id", "Name", "SKU", "Quantity", "Price")
                SELECT "Id", "Name", "SKU", "Quantity", "Price"
                FROM "Products";

                DROP TABLE "Products";
                ALTER TABLE "Products_WithoutCategories" RENAME TO "Products";
                """);

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
