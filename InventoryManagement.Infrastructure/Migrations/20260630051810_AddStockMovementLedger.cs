using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO StockMovements
                    (ProductId, MovementType, QuantityChange, BalanceBefore, BalanceAfter,
                     UnitCost, SourceType, SourceId, Reference, Note, OccurredAtUtc, CreatedBy)
                SELECT
                    Id, 0, Quantity, 0, Quantity,
                    AverageCost, 'Migration', NULL, 'Opening stock', NULL, CURRENT_TIMESTAMP, 'migration'
                FROM Products
                WHERE Quantity <> 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "MovementType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "ProductId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");
        }
    }
}
