using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantScopeMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_Name",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_GstNumber",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_Name",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_GstNumber",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Name",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Units",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Products",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Drivers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CompanyProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "CompanyProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                DELETE FROM "Units"
                WHERE NOT EXISTS (SELECT 1 FROM "Companies")
                  AND NOT EXISTS (SELECT 1 FROM "Categories")
                  AND NOT EXISTS (SELECT 1 FROM "Products")
                  AND NOT EXISTS (SELECT 1 FROM "Customers")
                  AND NOT EXISTS (SELECT 1 FROM "Suppliers")
                  AND NOT EXISTS (SELECT 1 FROM "Drivers")
                  AND NOT EXISTS (SELECT 1 FROM "CompanyProfiles");
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Companies" ("Name", "CreatedAtUtc")
                SELECT 'Default Company', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM "Companies")
                  AND (
                    EXISTS (SELECT 1 FROM "Categories") OR
                    EXISTS (SELECT 1 FROM "Units") OR
                    EXISTS (SELECT 1 FROM "Products") OR
                    EXISTS (SELECT 1 FROM "Customers") OR
                    EXISTS (SELECT 1 FROM "Suppliers") OR
                    EXISTS (SELECT 1 FROM "Drivers") OR
                    EXISTS (SELECT 1 FROM "CompanyProfiles")
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Categories"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "Units"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "Products"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "Customers"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "Suppliers"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "Drivers"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;

                UPDATE "CompanyProfiles"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Units",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Suppliers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Drivers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "CompanyProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                SELECT setval(
                    pg_get_serial_sequence('"CompanyProfiles"', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM "CompanyProfiles"), 1), 1),
                    EXISTS (SELECT 1 FROM "CompanyProfiles"));
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Categories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_CompanyId_Name",
                table: "Units",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyId_GstNumber",
                table: "Suppliers",
                columns: new[] { "CompanyId", "GstNumber" },
                unique: true,
                filter: "\"GstNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CompanyId_Name",
                table: "Suppliers",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Name",
                table: "Products",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_SKU",
                table: "Products",
                columns: new[] { "CompanyId", "SKU" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CompanyId_Name",
                table: "Drivers",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_GstNumber",
                table: "Customers",
                columns: new[] { "CompanyId", "GstNumber" },
                unique: true,
                filter: "\"GstNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_Name",
                table: "Customers",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CompanyId",
                table: "CompanyProfiles",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId_Name",
                table: "Categories",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_Companies_CompanyId",
                table: "CompanyProfiles",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Companies_CompanyId",
                table: "Units",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_Companies_CompanyId",
                table: "CompanyProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Companies_CompanyId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_CompanyId_Name",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CompanyId_GstNumber",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CompanyId_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CompanyId_Name",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_GstNumber",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_Name",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_CompanyId",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CompanyId_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "SKU",
                table: "Products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CompanyProfiles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_GstNumber",
                table: "Suppliers",
                column: "GstNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_Name",
                table: "Drivers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GstNumber",
                table: "Customers",
                column: "GstNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
