using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantScopeTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_PurchaseId_ReturnDate",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_ReturnNumber",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementType_OccurredAtUtc",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_InvoiceNumber",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierId_SupplierBillNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SupplierId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_DocumentSequences_DocumentType_Year",
                table: "DocumentSequences");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_ChallanNumber",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_SalesInvoiceId_ReturnDate",
                table: "CustomerReturns");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SupplierReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Purchases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DocumentSequences",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DeliveryChallans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "CustomerReturns",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Companies" ("Name", "CreatedAtUtc")
                SELECT 'Default Company', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM "Companies")
                  AND (
                    EXISTS (SELECT 1 FROM "Purchases") OR
                    EXISTS (SELECT 1 FROM "SupplierReturns") OR
                    EXISTS (SELECT 1 FROM "SalesInvoices") OR
                    EXISTS (SELECT 1 FROM "CustomerReturns") OR
                    EXISTS (SELECT 1 FROM "DeliveryChallans") OR
                    EXISTS (SELECT 1 FROM "Payments") OR
                    EXISTS (SELECT 1 FROM "StockMovements") OR
                    EXISTS (SELECT 1 FROM "DocumentSequences")
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Purchases" p
                SET "CompanyId" = COALESCE(
                    (SELECT s."CompanyId" FROM "Suppliers" s WHERE s."Id" = p."SupplierId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE p."CompanyId" IS NULL;

                UPDATE "SalesInvoices" si
                SET "CompanyId" = COALESCE(
                    (SELECT cu."CompanyId" FROM "Customers" cu WHERE cu."Id" = si."CustomerId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE si."CompanyId" IS NULL;

                UPDATE "DeliveryChallans" dc
                SET "CompanyId" = COALESCE(
                    (SELECT cu."CompanyId" FROM "Customers" cu WHERE cu."Id" = dc."CustomerId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE dc."CompanyId" IS NULL;

                UPDATE "SupplierReturns" sr
                SET "CompanyId" = COALESCE(
                    (SELECT p."CompanyId" FROM "Purchases" p WHERE p."Id" = sr."PurchaseId"),
                    (SELECT s."CompanyId" FROM "Suppliers" s WHERE s."Id" = sr."SupplierId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE sr."CompanyId" IS NULL;

                UPDATE "CustomerReturns" cr
                SET "CompanyId" = COALESCE(
                    (SELECT si."CompanyId" FROM "SalesInvoices" si WHERE si."Id" = cr."SalesInvoiceId"),
                    (SELECT cu."CompanyId" FROM "Customers" cu WHERE cu."Id" = cr."CustomerId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE cr."CompanyId" IS NULL;

                UPDATE "Payments" py
                SET "CompanyId" = COALESCE(
                    (SELECT si."CompanyId" FROM "SalesInvoices" si WHERE si."Id" = py."SalesInvoiceId"),
                    (SELECT p."CompanyId" FROM "Purchases" p WHERE p."Id" = py."PurchaseId"),
                    (SELECT cu."CompanyId" FROM "Customers" cu WHERE cu."Id" = py."CustomerId"),
                    (SELECT s."CompanyId" FROM "Suppliers" s WHERE s."Id" = py."SupplierId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE py."CompanyId" IS NULL;

                UPDATE "StockMovements" sm
                SET "CompanyId" = COALESCE(
                    (SELECT p."CompanyId" FROM "Products" p WHERE p."Id" = sm."ProductId"),
                    (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1))
                WHERE sm."CompanyId" IS NULL;

                UPDATE "DocumentSequences"
                SET "CompanyId" = (SELECT "Id" FROM "Companies" ORDER BY "Id" LIMIT 1)
                WHERE "CompanyId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "SupplierReturns",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "StockMovements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "SalesInvoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Purchases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "Payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "DocumentSequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "DeliveryChallans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "CustomerReturns",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CompanyId",
                table: "SupplierReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CompanyId_PurchaseId_ReturnDate",
                table: "SupplierReturns",
                columns: new[] { "CompanyId", "PurchaseId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CompanyId_ReturnNumber",
                table: "SupplierReturns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PurchaseId",
                table: "SupplierReturns",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId",
                table: "StockMovements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId_MovementType_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "CompanyId", "MovementType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId_ProductId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "CompanyId", "ProductId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CompanyId",
                table: "SalesInvoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CompanyId_InvoiceNumber",
                table: "SalesInvoices",
                columns: new[] { "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CompanyId_PurchaseNumber",
                table: "Purchases",
                columns: new[] { "CompanyId", "PurchaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_CompanyId_SupplierId_SupplierBillNumber",
                table: "Purchases",
                columns: new[] { "CompanyId", "SupplierId", "SupplierBillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_CustomerId_PaymentDate",
                table: "Payments",
                columns: new[] { "CompanyId", "CustomerId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_PurchaseId",
                table: "Payments",
                columns: new[] { "CompanyId", "PurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_ReceiptNumber",
                table: "Payments",
                columns: new[] { "CompanyId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_SalesInvoiceId",
                table: "Payments",
                columns: new[] { "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_SupplierId_PaymentDate",
                table: "Payments",
                columns: new[] { "CompanyId", "SupplierId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SupplierId",
                table: "Payments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequences_CompanyId_DocumentType_Year",
                table: "DocumentSequences",
                columns: new[] { "CompanyId", "DocumentType", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_CompanyId",
                table: "DeliveryChallans",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_CompanyId_ChallanNumber",
                table: "DeliveryChallans",
                columns: new[] { "CompanyId", "ChallanNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CompanyId",
                table: "CustomerReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CompanyId_ReturnNumber",
                table: "CustomerReturns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_CompanyId_SalesInvoiceId_ReturnDate",
                table: "CustomerReturns",
                columns: new[] { "CompanyId", "SalesInvoiceId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_SalesInvoiceId",
                table: "CustomerReturns",
                column: "SalesInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReturns_Companies_CompanyId",
                table: "CustomerReturns",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallans_Companies_CompanyId",
                table: "DeliveryChallans",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentSequences_Companies_CompanyId",
                table: "DocumentSequences",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Companies_CompanyId",
                table: "Payments",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Companies_CompanyId",
                table: "Purchases",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Companies_CompanyId",
                table: "SalesInvoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Companies_CompanyId",
                table: "StockMovements",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierReturns_Companies_CompanyId",
                table: "SupplierReturns",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReturns_Companies_CompanyId",
                table: "CustomerReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallans_Companies_CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentSequences_Companies_CompanyId",
                table: "DocumentSequences");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Companies_CompanyId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Companies_CompanyId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Companies_CompanyId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Companies_CompanyId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierReturns_Companies_CompanyId",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_CompanyId",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_CompanyId_PurchaseId_ReturnDate",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_CompanyId_ReturnNumber",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_SupplierReturns_PurchaseId",
                table: "SupplierReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CompanyId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CompanyId_MovementType_OccurredAtUtc",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CompanyId_ProductId_OccurredAtUtc",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_CompanyId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_CompanyId_InvoiceNumber",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_CompanyId_PurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_CompanyId_SupplierId_SupplierBillNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_CustomerId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_PurchaseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_SalesInvoiceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CompanyId_SupplierId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SupplierId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_DocumentSequences_CompanyId_DocumentType_Year",
                table: "DocumentSequences");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_CompanyId_ChallanNumber",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_CompanyId",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_CompanyId_ReturnNumber",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_CompanyId_SalesInvoiceId_ReturnDate",
                table: "CustomerReturns");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReturns_SalesInvoiceId",
                table: "CustomerReturns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SupplierReturns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DocumentSequences");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CustomerReturns");

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
                name: "IX_StockMovements_MovementType_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "MovementType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "ProductId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_InvoiceNumber",
                table: "SalesInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseNumber",
                table: "Purchases",
                column: "PurchaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId_SupplierBillNumber",
                table: "Purchases",
                columns: new[] { "SupplierId", "SupplierBillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_PaymentDate",
                table: "Payments",
                columns: new[] { "CustomerId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SupplierId_PaymentDate",
                table: "Payments",
                columns: new[] { "SupplierId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequences_DocumentType_Year",
                table: "DocumentSequences",
                columns: new[] { "DocumentType", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_ChallanNumber",
                table: "DeliveryChallans",
                column: "ChallanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_ReturnNumber",
                table: "CustomerReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_SalesInvoiceId_ReturnDate",
                table: "CustomerReturns",
                columns: new[] { "SalesInvoiceId", "ReturnDate" });
        }
    }
}
