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
        public async Task Create_Then_Get_Should_Return_Draft_Without_Changing_Stock()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();

            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new Command
                {
                    InvoiceNumber = $"INV-{Guid.NewGuid():N}",
                    CustomerId = seed.CustomerId,
                    InvoiceDate = new DateTime(2026, 7, 1),
                    Discount = 5,
                    OtherCharges = 2,
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
            created!.Status.Should().Be(SalesInvoiceStatus.Draft);
            created.Subtotal.Should().Be(100);
            created.TaxAmount.Should().Be(18);
            created.GrandTotal.Should().Be(115);
            created.AmountPaid.Should().Be(0);
            created.BalanceDue.Should().Be(115);
            created.Notes.Should().Be("Draft invoice");
            created.Items.Should().ContainSingle();
            created.Items[0].LineTotal.Should().Be(118);
            created.Items[0].CostAtSale.Should().BeNull();
            created.Items[0].DeliveryChallanItemId.Should().BeNull();

            var getResponse = await Client.GetAsync(
                $"/api/sales-invoices/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            fetched.Should().BeEquivalentTo(created);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId);
            product.Quantity.Should().Be(seed.StockQuantity);
            (await db.StockMovements.CountAsync(x => x.ProductId == seed.ProductId))
                .Should().Be(seed.StockMovementCount);
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
            body.Should().Contain("traceId");
        }

        [Fact]
        public async Task List_Should_Page_And_Apply_All_Filters()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var matchingNumber = $"MATCH-{Guid.NewGuid():N}";
            await CreateInvoiceAsync(
                seed,
                matchingNumber,
                new DateTime(2026, 7, 10));
            await CreateInvoiceAsync(
                seed,
                $"OTHER-{Guid.NewGuid():N}",
                new DateTime(2026, 6, 1));

            var response = await Client.GetAsync(
                $"/api/sales-invoices?pageNumber=1&pageSize=1" +
                $"&customerId={seed.CustomerId}&status=Draft" +
                "&dateFrom=2026-07-01&dateTo=2026-07-31" +
                $"&invoiceNumber={matchingNumber[..12]}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await response.Content
                .ReadFromJsonAsync<PagedResponse<SalesInvoiceResponse>>();
            page.Should().NotBeNull();
            page!.Items.Should().ContainSingle(x =>
                x.InvoiceNumber == matchingNumber);
            page.PageNumber.Should().Be(1);
            page.PageSize.Should().Be(1);
            page.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Update_Draft_Should_Recalculate_Totals_And_Reject_Paid_Invoice()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var created = await CreateInvoiceAsync(
                seed,
                $"EDIT-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));
            var update = new UpdateSalesInvoiceCommand(
                0,
                $"EDITED-{Guid.NewGuid():N}",
                seed.CustomerId,
                new DateTime(2026, 7, 2),
                4,
                2,
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
            updated.Subtotal.Should().Be(60);
            updated.TaxAmount.Should().Be(3);
            updated.GrandTotal.Should().Be(61);
            updated.BalanceDue.Should().Be(61);
            updated.AmountPaid.Should().Be(0);
            updated.Notes.Should().Be("Edited draft");
            updated.Items.Should().ContainSingle();
            updated.Items[0].CostAtSale.Should().BeNull();
            updated.CreatedAtUtc.Should().Be(created.CreatedAtUtc);
            updated.CreatedBy.Should().Be(created.CreatedBy);
            updated.UpdatedAtUtc.Should().BeAfter(created.UpdatedAtUtc);

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
                    .BalanceDue.Should().Be(invoice.GrandTotal);

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
        public async Task Post_Should_Reject_Aggregate_Insufficient_Stock_Atomically()
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
            createResponse.EnsureSuccessStatusCode();
            var invoice = await createResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            invoice.Should().NotBeNull();

            var response = await Client.PostAsync(
                $"/api/sales-invoices/{invoice!.Id}/post",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Products.AsNoTracking()
                .SingleAsync(x => x.Id == seed.ProductId))
                .Quantity.Should().Be(seed.StockQuantity);
            (await db.Customers.AsNoTracking()
                .SingleAsync(x => x.Id == seed.CustomerId))
                .BalanceDue.Should().Be(0);
            var persisted = await db.SalesInvoices.AsNoTracking()
                .Include(x => x.Items)
                .SingleAsync(x => x.Id == invoice.Id);
            persisted.Status.Should().Be(SalesInvoiceStatus.Draft);
            persisted.PostedAtUtc.Should().BeNull();
            persisted.Items.Should().OnlyContain(x => x.CostAtSale == null);
            (await db.StockMovements.CountAsync(x =>
                x.SourceType == "SalesInvoice" &&
                x.SourceId == invoice.Id.ToString())).Should().Be(0);
        }

        [Fact]
        public async Task Post_Should_Roll_Back_When_A_Movement_Insert_Fails()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var invoice = await CreateInvoiceAsync(
                seed,
                $"ROLLBACK-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 1));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                await db.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TRIGGER FailSalesInvoiceMovement
                    BEFORE INSERT ON StockMovements
                    BEGIN
                        SELECT RAISE(ABORT, 'forced sales posting failure');
                    END;
                    """);
            }

            try
            {
                var response = await Client.PostAsync(
                    $"/api/sales-invoices/{invoice.Id}/post",
                    null);
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
                var persisted = await db.SalesInvoices.AsNoTracking()
                    .Include(x => x.Items)
                    .SingleAsync(x => x.Id == invoice.Id);
                persisted.Status.Should().Be(SalesInvoiceStatus.Draft);
                persisted.Items.Should().OnlyContain(x => x.CostAtSale == null);
                (await db.StockMovements.CountAsync(x =>
                    x.SourceType == "SalesInvoice" &&
                    x.SourceId == invoice.Id.ToString())).Should().Be(0);
            }
            finally
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                await db.Database.ExecuteSqlRawAsync(
                    "DROP TRIGGER IF EXISTS FailSalesInvoiceMovement;");
            }
        }

        [Fact]
        public async Task Challan_Invoice_Should_Create_Debt_Without_Reducing_Stock_Twice()
        {
            await AuthenticateAsync();
            var seed = await SeedDependenciesAsync();
            var firstChallan = await CreateAndPostChallanAsync(
                seed, 2, $"DC-A-{Guid.NewGuid():N}");
            var secondChallan = await CreateAndPostChallanAsync(
                seed, 3, $"DC-B-{Guid.NewGuid():N}");

            int firstItemId;
            int secondItemId;
            decimal stockAfterChallans;
            int movementCountAfterChallans;
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
            }

            var createResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices/from-challans",
                new InventoryManagement.Application.Features.SalesInvoices
                    .CreateFromChallans.Command
                {
                    InvoiceNumber = $"DC-INV-{Guid.NewGuid():N}",
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
            draft!.Items.Select(x => x.Quantity).Should().Equal(2, 3);
            draft.Items.Select(x => x.DeliveryChallanItemId)
                .Should().Equal(firstItemId, secondItemId);

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

            var postResponse = await Client.PostAsync(
                $"/api/sales-invoices/{draft.Id}/post",
                null);
            postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var posted = await postResponse.Content
                .ReadFromJsonAsync<SalesInvoiceResponse>();
            posted.Should().NotBeNull();
            posted!.Items.Should().OnlyContain(x => x.CostAtSale == 25);

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
                .BalanceDue.Should().Be(draft.GrandTotal);
            var challans = await verificationDb.DeliveryChallans.AsNoTracking()
                .Where(x => x.Id == firstChallan.Id || x.Id == secondChallan.Id)
                .ToListAsync();
            challans.Should().OnlyContain(x =>
                x.Status == DeliveryChallanStatus.Invoiced &&
                x.InvoicedAtUtc != null);
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
                        Quantity = 2.5m
                    }
                }
            };
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
                AverageCost = averageCost,
                Category = new Category
                {
                    Name = $"Additional invoice category {suffix}",
                    Description = "Test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return new ProductSeedResult(product.Id);
        }

        private async Task<DeliveryChallanResponse> CreateAndPostChallanAsync(
            SeedResult seed,
            decimal quantity,
            string challanNumber)
        {
            var createResponse = await Client.PostAsJsonAsync(
                "/api/delivery-challans",
                new CreateChallanCommand
                {
                    ChallanNumber = challanNumber,
                    CustomerId = seed.CustomerId,
                    ChallanDate = new DateTime(2026, 7, 1),
                    DeliveryAddress = "Test address",
                    Items =
                    {
                        new CreateChallanItemInput
                        {
                            ProductId = seed.ProductId,
                            Quantity = quantity
                        }
                    }
                });
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

        private sealed record SeedResult(
            int CustomerId,
            int ProductId,
            int DeliveryChallanItemId,
            decimal StockQuantity,
            int StockMovementCount);

        private sealed record ProductSeedResult(int ProductId);
    }
}
