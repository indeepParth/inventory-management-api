using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReturnNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", precision: 9, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnItems_PurchaseItems_PurchaseItemId",
                        column: x => x.PurchaseItemId,
                        principalTable: "PurchaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnItems_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_ProductId",
                table: "SupplierReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_PurchaseItemId",
                table: "SupplierReturnItems",
                column: "PurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnItems_SupplierReturnId",
                table: "SupplierReturnItems",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PurchaseId_ReturnDate",
                table: "SupplierReturns",
                columns: new[] { "PurchaseId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_ReturnNumber",
                table: "SupplierReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                table: "SupplierReturns",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierReturnItems");

            migrationBuilder.DropTable(
                name: "SupplierReturns");
        }
    }
}
