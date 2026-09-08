using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Common.Models;
using InventoryManagement.Application.Features.SalesInvoices;
using InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice;
using InventoryManagement.Application.Features.DeliveryChallans;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdateSalesInvoiceCommand =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.Command;
using UpdateSalesInvoiceItemInput =
    InventoryManagement.Application.Features.SalesInvoices.UpdateSalesInvoice.SalesInvoiceItemInput;
using CreateChallanCommand =
    InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan.Command;
using CreateChallanItemInput =
    InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan.DeliveryChallanItemInput;
using ChallanItemInput =
    InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans.ChallanItemInput;

namespace InventoryManagement.Tests.IntegrationTests.SalesInvoices
{
    public class SalesInvoiceEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public SalesInvoiceEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_Then_Get_Should_Return_Posted_With_Stock_And_Debt_Effects()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            const decimal existingBalance = 40m;
            await SetCustomerBalanceAsync(seed.CustomerId, existingBalance);
            var invoiceNumber = $"INV-{Guid.NewGuid():N}";

            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Discount = 5,
                    OtherCharges = 2,
                    DeliveryAddress = " Customer delivery site ",
                    Notes = " Draft invoice ",
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 2.5m,
                            SellingUnitPrice = 40,
                            TaxRate = 18
                        }
                    }
                });

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            created.Should().NotBeNull();
            created!.Status.Should().Be(SalesInvoiceStatus.Posted);
            created.InvoiceNumber.Should().Be(invoiceNumber);
            created.PostedAtUtc.Should().NotBeNull();
            created.Subtotal.Should().Be(100);
            created.TaxAmount.Should().Be(18);
            created.OtherCharges.Should().Be(0);
            created.LaborCharge.Should().Be(0);
            created.DriverId.Should().BeNull();
            created.DeliveryAddress.Should().Be("Customer delivery site");
            created.GrandTotal.Should().Be(113);
            created.AmountPaid.Should().Be(0);
            created.BalanceDue.Should().Be(113);
            created.Notes.Should().Be("Draft invoice");
            created.Items.Should().ContainSingle();
            created.Items[0].LineTotal.Should().Be(118);
            created.Items[0].CostAtSale.Should().Be(25);
            created.Items[0].DeliveryChallanItemId.Should().BeNull();

            var getResponse = await Client.GetAsync(
                $"/api/sales-invoices/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            fetched.Should().BeEquivalentTo(
                created,
                options => options
                    .Using<DateTime>(ctx => ctx.Subject.Should()
                        .BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1)))
                    .WhenTypeIs<DateTime>());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId);
            product.Quantity.Should().Be(10m);
            (await db.StockMovements.CountAsync(x => x.ProductId == seed.ProductId))
                .Should().Be(seed.StockMovementCount + 1);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(existingBalance + created.GrandTotal);
        }

        [Fact]
        public async Task Create_With_Driver_Should_Include_Driver_And_Labor_Charges()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            int driverId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var driver = new Driver
                {
                    Name = $"Invoice driver {Guid.NewGuid():N}",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.Drivers.Add(driver);
                await db.SaveChangesAsync();
                driverId = driver.Id;
            }

            var invoiceNumber = $"INV-DRIVER-{Guid.NewGuid():N}";
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = seed.CustomerId,
                    DriverId = driverId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    OtherCharges = 30,
                    LaborCharge = 12,
                    DeliveryAddress = "Driver delivery site",
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 2,
                            SellingUnitPrice = 50,
                            TaxRate = 18
                        }
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            created.Should().NotBeNull();
            created!.DriverId.Should().Be(driverId);
            created.InvoiceNumber.Should().Be(invoiceNumber);
            created.DeliveryAddress.Should().Be("Driver delivery site");
            created.Subtotal.Should().Be(100);
            created.TaxAmount.Should().Be(18);
            created.OtherCharges.Should().Be(30);
            created.LaborCharge.Should().Be(12);
            created.GrandTotal.Should().Be(160);
            created.BalanceDue.Should().Be(160);
        }

        [Fact]
        public async Task Create_Should_Return_Structured_Validation_For_Empty_Items()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = " ",
                    CustomerId = 0,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Items = new List<SalesInvoiceItemInput>()
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
            body.Should().Contain("InvoiceNumber");
            body.Should().Contain("traceId");
        }

        [Fact]
        public async Task Create_Should_Reject_Duplicate_Invoice_Number()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var invoiceNumber = $"DUPLICATE-{Guid.NewGuid():N}";
            await CreateInvoiceAsync(
                seed,
                invoiceNumber,
                new DateTime(2026, 7, 1));

            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 20
                        }
                    }
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Invoice number already exists.");
        }

        [Fact]
        public async Task List_Should_Page_And_Apply_All_Filters()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var matching = await CreateInvoiceAsync(
                seed,
                $"MATCH-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 10));
            await CreateInvoiceAsync(
                seed,
                $"OTHER-{Guid.NewGuid():N}",
                new DateTime(2026, 6, 1));

            var response = await Client.GetAsync(
                $"/api/sales-invoices?pageNumber=1&pageSize=1" +
                $"&customerId={seed.CustomerId}&status=Posted" +
                "&dateFrom=2026-07-01&dateTo=2026-07-31" +
                $"&invoiceNumber={matching.InvoiceNumber[..12]}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content
                .ReadFromJsonAsync<PagedResponse<SalesInvoiceResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x =>
                x.InvoiceNumber == matching.InvoiceNumber);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Update_Draft_Should_Recalculate_Totals_And_Reject_Paid_Invoice()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var created = await SeedDraftInvoiceAsync(
                seed,
                $"EDIT-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));
            var editedInvoiceNumber = $"EDITED-{Guid.NewGuid():N}";
            var update = new UpdateSalesInvoiceCommand(
                0,
                editedInvoiceNumber,
                seed.CustomerId,
                null,
                new DateTime(2026, 7, 2),
                4,
                2,
                0,
                " Edited delivery site ",
                " Edited draft ",
                new List<UpdateSalesInvoiceItemInput>
                {
                    new()
                    {
                        ProductId = seed.ProductId,
                        Quantity = 3,
                        SellingUnitPrice = 20,
                        TaxRate = 5
                    }
                });

            var response = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{created.Id}",
                update);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(SalesInvoiceStatus.Draft);
            updated.InvoiceNumber.Should().Be(editedInvoiceNumber);
            updated.Subtotal.Should().Be(60);
            updated.TaxAmount.Should().Be(3);
            updated.OtherCharges.Should().Be(0);
            updated.LaborCharge.Should().Be(0);
            updated.DriverId.Should().BeNull();
            updated.DeliveryAddress.Should().Be("Edited delivery site");
            updated.GrandTotal.Should().Be(59);
            updated.BalanceDue.Should().Be(59);
            updated.AmountPaid.Should().Be(0);
            updated.Notes.Should().Be("Edited draft");
            updated.Items.Should().ContainSingle();
            updated.Items[0].CostAtSale.Should().BeNull();
            updated.CreatedAtUtc.Should()
                .BeCloseTo(created.CreatedAtUtc, TimeSpan.FromMilliseconds(1));
            updated.CreatedBy.Should().Be(created.CreatedBy);
            updated.UpdatedAtUtc.Should().BeAfter(created.UpdatedAtUtc);

            var duplicate = await SeedDraftInvoiceAsync(
                seed,
                $"EDIT-DUPLICATE-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 3));
            var duplicateUpdate = update with
            {
                InvoiceNumber = duplicate.InvoiceNumber
            };
            var duplicateResponse = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{created.Id}",
                duplicateUpdate);
            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var duplicateBody = await duplicateResponse.Content.ReadAsStringAsync();
            duplicateBody.Should().Contain("Invoice number already exists.");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var invoice = await db.SalesInvoices
                    .SingleAsync(x => x.Id == created.Id);
                invoice.Status = SalesInvoiceStatus.Paid;
                await db.SaveChangesAsync();
            }

            var rejected = await Client.PutAsJsonAsync(
                $"/api/sales-invoices/{created.Id}",
                update);
            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_Direct_Invoice_Should_Update_Stock_Debt_And_Profit_Data_Once()
        {
            await AuthenticateAsync();
            var first = await SeedDependenciesAsync();
            var second = await SeedAdditionalProductAsync(8, 11);
            const decimal existingBalance = 25m;
            await SetCustomerBalanceAsync(first.CustomerId, existingBalance);
            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = $"POST-{Guid.NewGuid():N}",
                    CustomerId = first.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = first.ProductId,
                            Quantity = 2,
                            SellingUnitPrice = 40
                        },
                        new SalesInvoiceItemInput
                        {
                            ProductId = first.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 50
                        },
                        new SalesInvoiceItemInput
                        {
                            ProductId = second.ProductId,
                            Quantity = 3,
                            SellingUnitPrice = 20
                        }
                    }
                });
            createResponse.EnsureSuccessStatusCode();
            var invoice = await createResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            invoice.Should().NotBeNull();

            var postResponse = await Client.PostAsync(
                $"/api/sales-invoices/{invoice!.Id}/post",
                null);

            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var posted = await postResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            posted.Should().NotBeNull();
            posted!.Status.Should().Be(SalesInvoiceStatus.Posted);
            posted.PostedAtUtc.Should().NotBeNull();
            posted.Items.Where(x => x.ProductId == first.ProductId)
                .Should().OnlyContain(x => x.CostAtSale == 25);
            posted.Items.Single(x => x.ProductId == second.ProductId)
                .CostAtSale.Should().Be(11);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var firstProduct = await db.Products
                    .SingleAsync(x => x.Id == first.ProductId);
                var secondProduct = await db.Products
                    .SingleAsync(x => x.Id == second.ProductId);
                firstProduct.Quantity.Should().Be(9.5m);
                secondProduct.Quantity.Should().Be(5);
                (await db.Customers.SingleAsync(x => x.Id == first.CustomerId))
                    .BalanceDue.Should().Be(existingBalance + invoice.GrandTotal);

                var movements = await db.StockMovements.AsNoTracking()
                    .Where(x => x.SourceType == "SalesInvoice" &&
                                x.SourceId == invoice.Id.ToString())
                    .OrderBy(x => x.Id)
                    .ToListAsync();
                movements.Should().HaveCount(3);
                movements.Should().OnlyContain(x =>
                    x.MovementType == StockMovementType.Sale);
                movements.Select(x => x.QuantityChange)
                    .Should().Equal(-2, -1, -3);
                movements.Select(x => x.UnitCost)
                    .Should().Equal(25, 25, 11);

                firstProduct.AverageCost = 99;
                secondProduct.AverageCost = 88;
                await db.SaveChangesAsync();
            }

            var fetched = await Client.GetFromJsonAsync<SalesInvoiceResponse>(
                $"/api/sales-invoices/{invoice.Id}");
            fetched.Should().NotBeNull();
            fetched!.Items.Where(x => x.ProductId == first.ProductId)
                .Should().OnlyContain(x => x.CostAtSale == 25);
            fetched.Items.Single(x => x.ProductId == second.ProductId)
                .CostAtSale.Should().Be(11);

            var repeatedResponse = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/post",
                null);
            repeatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var repeated = await repeatedResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            repeated.Should().BeEquivalentTo(posted);

            using var verificationScope = _factory.Services.CreateScope();
            var verificationDb = verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await verificationDb.Products.AsNoTracking()
                .SingleAsync(x => x.Id == first.ProductId))
                .Quantity.Should().Be(9.5m);
            (await verificationDb.StockMovements.CountAsync(x =>
                x.SourceType == "SalesInvoice" &&
                x.SourceId == invoice.Id.ToString())).Should().Be(3);
        }

        [Fact]
        public async Task Post_Legacy_Draft_Should_Add_To_Existing_Customer_Balance()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            const decimal existingBalance = 30m;
            await SetCustomerBalanceAsync(seed.CustomerId, existingBalance);
            var invoice = await SeedDraftInvoiceAsync(
                seed,
                $"LEGACY-POST-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/post",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var posted = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            posted.Should().NotBeNull();
            posted!.Status.Should().Be(SalesInvoiceStatus.Posted);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(existingBalance + posted.GrandTotal);
        }

        [Fact]
        public async Task Create_Should_Reject_Aggregate_Insufficient_Stock_Atomically()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = $"SHORT-{Guid.NewGuid():N}",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 7,
                            SellingUnitPrice = 10
                        },
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 6,
                            SellingUnitPrice = 10
                        }
                    }
                });
            createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId))
                .Quantity.Should().Be(seed.StockQuantity);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(0);
            (await db.SalesInvoices.CountAsync(x =>
                x.CustomerId == seed.CustomerId)).Should().Be(0);
            (await db.StockMovements.CountAsync(x =>
                x.SourceType == "SalesInvoice" &&
                x.ProductId == seed.ProductId)).Should().Be(0);
        }

        [Fact]
        public async Task Post_Should_Roll_Back_When_A_Movement_Insert_Fails()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE OR REPLACE FUNCTION fail_sales_invoice_movement()
                    RETURNS trigger
                    LANGUAGE plpgsql
                    AS $$
                    BEGIN
                        RAISE EXCEPTION 'forced sales posting failure';
                    END;
                    $$;

                    CREATE TRIGGER "FailSalesInvoiceMovement"
                    BEFORE INSERT ON "StockMovements"
                    FOR EACH ROW
                    EXECUTE FUNCTION fail_sales_invoice_movement();
                    """);
            }

            try
            {
                var response = await Client.PostAsJsonAsync(
                    "/api/sales-invoices",
                    new Command
                    {
                        InvoiceNumber = $"ROLLBACK-{Guid.NewGuid():N}",
                        CustomerId = seed.CustomerId,
                        InvoiceDate = new DateTime(2026, 7, 1),
                        Items =
                        {
                            new SalesInvoiceItemInput
                            {
                                ProductId = seed.ProductId,
                                Quantity = 1,
                                SellingUnitPrice = 10
                            }
                        }
                    });
                response.StatusCode.Should()
                    .Be(HttpStatusCode.InternalServerError);

                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId))
                    .Quantity.Should().Be(seed.StockQuantity);
                (await db.Customers.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue.Should().Be(0);
                (await db.SalesInvoices.CountAsync(x =>
                    x.CustomerId == seed.CustomerId)).Should().Be(0);
                (await db.StockMovements.CountAsync(x =>
                    x.SourceType == "SalesInvoice" &&
                    x.ProductId == seed.ProductId)).Should().Be(0);
            }
            finally
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                await db.Database.ExecuteSqlRawAsync(
                    """
                    DROP TRIGGER IF EXISTS "FailSalesInvoiceMovement" ON "StockMovements";
                    DROP FUNCTION IF EXISTS fail_sales_invoice_movement();
                    """);
            }
        }

        [Fact]
        public async Task Challan_Invoice_Should_Create_Debt_Without_Reducing_Stock_Twice()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var firstChallan = await CreateAndPostChallanAsync(
                seed, 2, $"DC-A-{Guid.NewGuid():N}", deliveryCharge: 30);
            var secondChallan = await CreateAndPostChallanAsync(
                seed, 3, $"DC-B-{Guid.NewGuid():N}", deliveryCharge: 40);

            int firstItemId;
            int secondItemId;
            decimal stockAfterChallans;
            int movementCountAfterChallans;
            const decimal existingBalance = 55m;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                firstItemId = await db.DeliveryChallanItems
                    .Where(x => x.DeliveryChallanId == firstChallan.Id)
                    .Select(x => x.Id)
                    .SingleAsync();
                secondItemId = await db.DeliveryChallanItems
                    .Where(x => x.DeliveryChallanId == secondChallan.Id)
                    .Select(x => x.Id)
                    .SingleAsync();
                stockAfterChallans = (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId)).Quantity;
                movementCountAfterChallans = await db.StockMovements.CountAsync(
                    x => x.ProductId == seed.ProductId);
                (await db.Customers.SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue = existingBalance;
                await db.SaveChangesAsync();
            }

            var invoiceNumber = $"DC-INV-{Guid.NewGuid():N}";
            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = firstItemId,
                            SellingUnitPrice = 40,
                            TaxRate = 5
                        },
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = secondItemId,
                            SellingUnitPrice = 50,
                            TaxRate = 10
                        }
                    }
                });

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var draft = await createResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            draft.Should().NotBeNull();
            draft!.Status.Should().Be(SalesInvoiceStatus.Posted);
            draft.InvoiceNumber.Should().Be(invoiceNumber);
            draft.PostedAtUtc.Should().NotBeNull();
            draft.Items.Select(x => x.Quantity).Should().Equal(2, 3);
            draft.Items.Select(x => x.DeliveryChallanItemId)
                .Should().Equal(firstItemId, secondItemId);
            draft.Items.Should().OnlyContain(x => x.CostAtSale == 25);
            draft.OtherCharges.Should().Be(70);
            draft.GrandTotal.Should().Be(319);
            draft.BalanceDue.Should().Be(319);

            var duplicateResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"DUP-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = firstItemId,
                            SellingUnitPrice = 40
                        }
                    }
                });
            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var verificationScope = _factory.Services.CreateScope();
            var verificationDb = verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await verificationDb.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId))
                .Quantity.Should().Be(stockAfterChallans);
            (await verificationDb.StockMovements.CountAsync(
                x => x.ProductId == seed.ProductId))
                .Should().Be(movementCountAfterChallans);
            (await verificationDb.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(existingBalance + draft.GrandTotal);
            var challans = await verificationDb.DeliveryChallans.AsNoTracking()
                .Where(x => x.Id == firstChallan.Id || x.Id == secondChallan.Id)
                .ToListAsync();
            challans.Should().OnlyContain(x =>
                x.Status == DeliveryChallanStatus.Invoiced &&
                x.InvoicedAtUtc != null);
        }

        [Fact]
        public async Task Challan_Invoice_Should_Bill_Entered_Quantity_And_Not_Converted_Base_Quantity()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            int convertedUnitId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var product = await db.Products
                    .SingleAsync(x => x.Id == seed.ProductId);
                product.Quantity = 20;
                var convertedUnit = new Unit
                {
                    Name = $"Invoice conversion bag {Guid.NewGuid():N}",
                    ShortName = "bag",
                    BaseUnitId = 1,
                    FactorToBaseUnit = 4,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.Units.Add(convertedUnit);
                await db.SaveChangesAsync();
                convertedUnitId = convertedUnit.Id;
            }

            var challanCreate = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                new CreateChallanCommand
                {
                    CustomerId = seed.CustomerId,
                    ChallanDate = new DateTime(2026, 7, 1),
                    DeliveryFromAddress = "Dispatch warehouse",
                    DeliveryAddress = "Test address",
                    Items =
                    {
                        new CreateChallanItemInput
                        {
                            ProductId = seed.ProductId,
                            EnteredQuantity = 2,
                            UnitId = convertedUnitId
                        }
                    }
                });
            challanCreate.EnsureSuccessStatusCode();
            var challan = (await challanCreate.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>())!;

            var challanPost = await Client.PostAsync(
                $"/api/delivery-challans/{challan.Id}/post",
                null);
            challanPost.EnsureSuccessStatusCode();

            decimal stockAfterChallan;
            int movementCountAfterChallan;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                stockAfterChallan = (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId)).Quantity;
                stockAfterChallan.Should().Be(12);
                movementCountAfterChallan = await db.StockMovements.CountAsync(
                    x => x.ProductId == seed.ProductId);
            }

            var invoiceCreate = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"ENTERED-QTY-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = challan.Items.Single().Id,
                            SellingUnitPrice = 100,
                            TaxRate = 0
                        }
                    }
                });

            invoiceCreate.StatusCode.Should().Be(HttpStatusCode.Created);
            var draft = (await invoiceCreate.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
            draft.Status.Should().Be(SalesInvoiceStatus.Posted);
            draft.InvoiceNumber.Should().StartWith("ENTERED-QTY-");
            draft.Items.Should().ContainSingle(x =>
                x.Quantity == 2 &&
                x.SellingUnitPrice == 100 &&
                x.LineTotal == 200);
            draft.Subtotal.Should().Be(200);
            draft.GrandTotal.Should().Be(200);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId))
                    .Quantity.Should().Be(stockAfterChallan);
                (await db.StockMovements.CountAsync(
                    x => x.ProductId == seed.ProductId))
                    .Should().Be(movementCountAfterChallan);
            }
        }

        [Fact]
        public async Task Challan_Invoice_Should_Apply_Delivery_Charge_Rules()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var secondProduct = await SeedAdditionalProductAsync(8, 11);
            var chargedChallan = await CreateAndPostChallanAsync(
                seed,
                2,
                $"DC-CHARGE-{Guid.NewGuid():N}",
                deliveryCharge: 25,
                secondProductId: secondProduct.ProductId,
                secondQuantity: 3);

            var chargedResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"DC-CHARGE-INV-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 2),
                    OtherCharges = 99,
                    Items = chargedChallan.Items.Select(x => new ChallanItemInput
                    {
                        DeliveryChallanItemId = x.Id,
                        SellingUnitPrice = 10
                    }).ToList()
                });

            chargedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var chargedInvoice = await chargedResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            chargedInvoice.Should().NotBeNull();
            chargedInvoice!.OtherCharges.Should().Be(25);
            chargedInvoice.Subtotal.Should().Be(50);
            chargedInvoice.GrandTotal.Should().Be(75);
            chargedInvoice.BalanceDue.Should().Be(75);

            var zeroChargeChallan = await CreateAndPostChallanAsync(
                seed, 1, $"DC-ZERO-{Guid.NewGuid():N}");
            var zeroChargeResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"DC-ZERO-INV-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 2),
                    OtherCharges = 12,
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = zeroChargeChallan.Items.Single().Id,
                            SellingUnitPrice = 10
                        }
                    }
                });

            zeroChargeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var zeroChargeInvoice = await zeroChargeResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            zeroChargeInvoice.Should().NotBeNull();
            zeroChargeInvoice!.OtherCharges.Should().Be(12);
            zeroChargeInvoice.GrandTotal.Should().Be(22);
            zeroChargeInvoice.BalanceDue.Should().Be(22);
        }

        [Fact]
        public async Task Challan_Invoice_Should_Reject_Different_Customers()
        {
            await AuthenticateAsync();
            var first = await SeedDependenciesAsync();
            var second = await SeedDependenciesAsync();
            var firstChallan = await CreateAndPostChallanAsync(
                first, 1, $"DC-C-{Guid.NewGuid():N}");
            var secondChallan = await CreateAndPostChallanAsync(
                second, 1, $"DC-D-{Guid.NewGuid():N}");

            List<int> itemIds;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                itemIds = await db.DeliveryChallanItems
                    .Where(x => x.DeliveryChallanId == firstChallan.Id ||
                                x.DeliveryChallanId == secondChallan.Id)
                    .Select(x => x.Id)
                    .ToListAsync();
            }

            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"MIXED-CUSTOMER-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items = itemIds.Select(x => new ChallanItemInput
                    {
                        DeliveryChallanItemId = x,
                        SellingUnitPrice = 10
                    }).ToList()
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cancel_Draft_Should_Have_No_Stock_Or_Debt_Effects()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var invoice = await SeedDraftInvoiceAsync(
                seed,
                $"CANCEL-DRAFT-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/cancel",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var cancelled = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            cancelled.Should().NotBeNull();
            cancelled!.Status.Should().Be(SalesInvoiceStatus.Cancelled);
            cancelled.CancelledAtUtc.Should().NotBeNull();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId))
                .Quantity.Should().Be(seed.StockQuantity);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(0);
            (await db.StockMovements.CountAsync(x =>
                x.SourceId == invoice.Id.ToString() &&
                x.SourceType.Contains("SalesInvoice"))).Should().Be(0);
        }

        [Fact]
        public async Task Cancel_Posted_Direct_Should_Restore_Stock_Debt_And_Be_Idempotent()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var invoice = await CreateInvoiceAsync(
                seed,
                $"CANCEL-DIRECT-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));
            (await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/post",
                null)).EnsureSuccessStatusCode();

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/cancel",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var cancelled = await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            cancelled.Should().NotBeNull();
            cancelled!.Status.Should().Be(SalesInvoiceStatus.Cancelled);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId))
                    .Quantity.Should().Be(seed.StockQuantity);
                (await db.Customers.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue.Should().Be(0);
                var movements = await db.StockMovements.AsNoTracking()
                    .Where(x => x.SourceId == invoice.Id.ToString() &&
                        (x.SourceType == "SalesInvoice" ||
                         x.SourceType == "SalesInvoiceCancellation"))
                    .OrderBy(x => x.Id)
                    .ToListAsync();
                movements.Should().HaveCount(2);
                movements[0].MovementType.Should().Be(StockMovementType.Sale);
                movements[1].MovementType.Should().Be(StockMovementType.Reversal);
                movements[1].QuantityChange
                    .Should().Be(-movements[0].QuantityChange);
            }

            var repeated = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/cancel",
                null);
            repeated.StatusCode.Should().Be(HttpStatusCode.OK);

            using var verificationScope = _factory.Services.CreateScope();
            var verificationDb = verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            (await verificationDb.StockMovements.CountAsync(x =>
                x.SourceId == invoice.Id.ToString() &&
                (x.SourceType == "SalesInvoice" ||
                 x.SourceType == "SalesInvoiceCancellation"))).Should().Be(2);
        }

        [Fact]
        public async Task Cancel_Challan_Invoice_Should_Release_Challan_Without_Stock_Change()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var challan = await CreateAndPostChallanAsync(
                seed, 2, $"DC-CANCEL-{Guid.NewGuid():N}");

            int challanItemId;
            decimal stockAfterChallan;
            int movementCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                challanItemId = await db.DeliveryChallanItems
                    .Where(x => x.DeliveryChallanId == challan.Id)
                    .Select(x => x.Id)
                    .SingleAsync();
                stockAfterChallan = (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId)).Quantity;
                movementCount = await db.StockMovements.CountAsync(
                    x => x.ProductId == seed.ProductId);
            }

            var draft = await CreateChallanInvoiceAsync(
                challanItemId,
                $"DC-CANCEL-INV-{Guid.NewGuid():N}");

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{draft.Id}/cancel",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                (await db.Products.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.ProductId))
                    .Quantity.Should().Be(stockAfterChallan);
                (await db.StockMovements.CountAsync(
                    x => x.ProductId == seed.ProductId))
                    .Should().Be(movementCount);
                (await db.Customers.AsNoTracking()
                    .SingleAsync(x => x.Id == seed.CustomerId))
                    .BalanceDue.Should().Be(0);
                var restoredChallan = await db.DeliveryChallans.AsNoTracking()
                    .SingleAsync(x => x.Id == challan.Id);
                restoredChallan.Status.Should().Be(DeliveryChallanStatus.Posted);
                restoredChallan.InvoicedAtUtc.Should().BeNull();
                (await db.SalesInvoiceItems.AsNoTracking()
                    .SingleAsync(x => x.SalesInvoiceId == draft.Id))
                    .IsChallanAllocationActive.Should().BeFalse();
            }

            var replacement = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"REINVOICE-{Guid.NewGuid():N}",
                    InvoiceDate = new DateTime(2026, 7, 3),
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = challanItemId,
                            SellingUnitPrice = 45
                        }
                    }
                });
            replacement.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Cancel_Paid_Invoice_Should_Be_Rejected()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var invoice = await CreateInvoiceAsync(
                seed,
                $"PAID-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var persisted = await db.SalesInvoices
                    .SingleAsync(x => x.Id == invoice.Id);
                persisted.Status = SalesInvoiceStatus.Paid;
                await db.SaveChangesAsync();
            }

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice.Id}/cancel",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task<SeedResult> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var customer = new Customer
            {
                Name = $"Invoice customer {suffix}",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var product = new Product
            {
                Name = $"Invoice product {suffix}",
                SKU = $"INV-{suffix}",
                Quantity = 12.5m,
                BaseUnitId = 1,
                AverageCost = 25,
                Category = new Category
                {
                    Name = $"Invoice category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            var challan = new DeliveryChallan
            {
                ChallanNumber = $"DC-{suffix}",
                Customer = customer,
                ChallanDate = new DateTime(2026, 7, 1),
                Status = DeliveryChallanStatus.Posted,
                DeliveryAddress = "Test address",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test",
                Items =
                {
                    new DeliveryChallanItem
                    {
                        Product = product,
                        EnteredQuantity = 2.5m,
                        UnitId = 1,
                        ConvertedBaseQuantity = 2.5m
                    }
                }
            };
            db.ProductUnitConversions.Add(new ProductUnitConversion
            {
                Product = product,
                UnitId = 1,
                FactorToBaseUnit = 1,
                IsActive = true
            });
            db.DeliveryChallans.Add(challan);
            await db.SaveChangesAsync();

            return new SeedResult(
                customer.Id,
                product.Id,
                challan.Items.Single().Id,
                product.Quantity,
                await db.StockMovements.CountAsync(x => x.ProductId == product.Id));
        }

        private async Task<SalesInvoiceResponse> CreateInvoiceAsync(
            SeedResult seed,
            string invoiceNumber,
            DateTime invoiceDate)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = seed.CustomerId,
                    InvoiceDate = invoiceDate,
                    Items =
                    {
                        new SalesInvoiceItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = 1,
                            SellingUnitPrice = 10
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private async Task SetCustomerBalanceAsync(
            int customerId,
            decimal balanceDue)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customer = await db.Customers.SingleAsync(x => x.Id == customerId);
            customer.BalanceDue = balanceDue;
            await db.SaveChangesAsync();
        }

        private async Task<SalesInvoiceResponse> SeedDraftInvoiceAsync(
            SeedResult seed,
            string invoiceNumber,
            DateTime invoiceDate)
        {
            int invoiceId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var customer = await db.Customers.SingleAsync(x => x.Id == seed.CustomerId);
                var product = await db.Products.SingleAsync(x => x.Id == seed.ProductId);
                var now = DateTime.UtcNow;
                var invoice = new SalesInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = customer.Id,
                    Customer = customer,
                    InvoiceDate = invoiceDate,
                    Status = SalesInvoiceStatus.Draft,
                    Subtotal = 10,
                    GrandTotal = 10,
                    BalanceDue = 10,
                    AmountPaid = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = "test",
                    Items =
                    {
                        new SalesInvoiceItem
                        {
                            ProductId = product.Id,
                            Product = product,
                            Quantity = 1,
                            SellingUnitPrice = 10,
                            LineTotal = 10
                        }
                    }
                };
                db.SalesInvoices.Add(invoice);
                await db.SaveChangesAsync();
                invoiceId = invoice.Id;
            }

            return (await Client.GetFromJsonAsync<SalesInvoiceResponse>(
                $"/api/sales-invoices/{invoiceId}"))!;
        }

        private async Task<ProductSeedResult> SeedAdditionalProductAsync(
            decimal quantity,
            decimal averageCost)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var product = new Product
            {
                Name = $"Additional invoice product {suffix}",
                SKU = $"INV-ADD-{suffix}",
                Quantity = quantity,
                BaseUnitId = 1,
                AverageCost = averageCost,
                Category = new Category
                {
                    Name = $"Additional invoice category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.ProductUnitConversions.Add(new ProductUnitConversion
            {
                Product = product,
                UnitId = 1,
                FactorToBaseUnit = 1,
                IsActive = true
            });
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return new ProductSeedResult(product.Id);
        }

        private async Task<DeliveryChallanResponse> CreateAndPostChallanAsync(
            SeedResult seed,
            decimal quantity,
            string challanNumber,
            decimal deliveryCharge = 0,
            int? secondProductId = null,
            decimal? secondQuantity = null)
        {
            var command = new CreateChallanCommand
            {
                ChallanNumber = challanNumber,
                CustomerId = seed.CustomerId,
                ChallanDate = new DateTime(2026, 7, 1),
                DeliveryFromAddress = "Dispatch warehouse",
                DeliveryAddress = "Test address",
                DeliveryCharge = deliveryCharge,
                Items =
                {
                    new CreateChallanItemInput
                    {
                        ProductId = seed.ProductId,
                        EnteredQuantity = quantity,
                        UnitId = 1
                    }
                }
            };
            if (secondProductId.HasValue && secondQuantity.HasValue)
            {
                command.Items.Add(new CreateChallanItemInput
                {
                    ProductId = secondProductId.Value,
                    EnteredQuantity = secondQuantity.Value,
                    UnitId = 1
                });
            }

            var createResponse = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                command);
            createResponse.EnsureSuccessStatusCode();
            var challan = await createResponse.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>();
            challan.Should().NotBeNull();
            var postResponse = await Client.PostAsync(
                $"/api/delivery-challans/{challan!.Id}/post",
                null);
            postResponse.EnsureSuccessStatusCode();
            return (await postResponse.Content
                .ReadFromJsonAsync<DeliveryChallanResponse>())!;
        }

        private async Task<SalesInvoiceResponse> CreateChallanInvoiceAsync(
            int challanItemId,
            string invoiceNumber)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = new DateTime(2026, 7, 2),
                    Items =
                    {
                        new ChallanItemInput
                        {
                            DeliveryChallanItemId = challanItemId,
                            SellingUnitPrice = 40
                        }
                    }
                });
            response.EnsureSuccessStatusCode();
            return (await response.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>())!;
        }

        private sealed record SeedResult(
            int CustomerId,
            int ProductId,
            int DeliveryChallanItemId,
            decimal StockQuantity,
            int StockMovementCount);

        private sealed record ProductSeedResult(int ProductId);
    }
}
