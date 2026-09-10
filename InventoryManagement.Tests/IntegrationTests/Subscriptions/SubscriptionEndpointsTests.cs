using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.DTOs.Subscription;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using CreateCompanyRequest = InventoryManagement.API.Controllers.CreateCompanyRequest;
using CreateCustomerCommand = InventoryManagement.Application.Features.Customers.CreateCustomer.Command;
using CreateProductCommand = InventoryManagement.Application.Features.Products.CreateProduct.Command;
using CreateSalesInvoiceCommand = InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.Command;
using CreateSalesInvoiceItemInput = InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice.SalesInvoiceItemInput;

namespace InventoryManagement.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionEndpointsTests : TestBase
    {
        private readonly CustomWebApplicationFactory _factory;

        public SubscriptionEndpointsTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Current_Should_Return_Free_Plan_And_Usage()
        {
            await AuthenticateAndCreateCompanyAsync();

            var response = await Client.GetAsync("/api/subscription/current");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var subscription = await response.Content
                .ReadFromJsonAsync<CurrentSubscriptionDto>();
            subscription.Should().NotBeNull();
            subscription!.Status.Should().Be(SubscriptionStatuses.Active);
            subscription.Plan.Code.Should().Be(SubscriptionPlans.Free);
            subscription.Plan.MaxCompanies.Should().Be(1);
            subscription.Plan.MaxUsersPerCompany.Should().Be(3);
            subscription.Plan.MaxInvoicesPerMonth.Should().Be(100);
            subscription.Plan.MaxProducts.Should().Be(500);
            subscription.Plan.MaxCustomers.Should().Be(500);
            subscription.Usage.OwnedCompanies.Should().Be(1);
            subscription.Usage.CompanyUsers.Should().Be(1);
            subscription.Usage.Products.Should().Be(0);
            subscription.Usage.Customers.Should().Be(0);
            subscription.Usage.InvoicesThisMonth.Should().Be(0);
        }

        [Fact]
        public async Task Current_Should_Return_Free_Plan_And_Zero_Owned_Companies_Before_First_Company()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync("/api/subscription/current");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var subscription = await response.Content
                .ReadFromJsonAsync<CurrentSubscriptionDto>();
            subscription.Should().NotBeNull();
            subscription!.Status.Should().Be(SubscriptionStatuses.Active);
            subscription.Plan.Code.Should().Be(SubscriptionPlans.Free);
            subscription.Usage.OwnedCompanies.Should().Be(0);
            subscription.Usage.CompanyUsers.Should().BeNull();
            subscription.Usage.Products.Should().BeNull();
            subscription.Usage.Customers.Should().BeNull();
            subscription.Usage.InvoicesThisMonth.Should().BeNull();
        }

        [Fact]
        public async Task CreateCompany_Should_Block_Second_Owned_Company_On_Free_Plan()
        {
            await AuthenticateAndCreateCompanyAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/companies",
                new CreateCompanyRequest
                {
                    Name = $"Blocked second company {Guid.NewGuid():N}"
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("allows up to 1 company");
        }

        [Fact]
        public async Task Invite_Should_Block_When_Free_Company_Has_Three_Reserved_Users()
        {
            await AuthenticateAndCreateCompanyAsync();
            await AddDirectCompanyMemberAsync(ActiveCompanyId);
            await AddDirectCompanyMemberAsync(ActiveCompanyId);

            var response = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = $"seat_limit_{Guid.NewGuid():N}@example.com",
                    Role = CompanyRoles.Staff
                });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("allows up to 3 users");
        }

        [Fact]
        public async Task Product_Customer_And_Invoice_Creation_Should_Respect_Limits()
        {
            await AuthenticateAndCreateCompanyAsync();

            await SetActiveCompanyBillingPlanLimitsAsync(maxProducts: 0);
            var productResponse = await Client.PostAsJsonAsync(
                "/api/products",
                new CreateProductCommand
                {
                    Name = $"Limited product {Guid.NewGuid():N}",
                    SKU = $"LIMIT-PROD-{Guid.NewGuid():N}",
                    BaseUnitId = 1,
                    DefaultSellingPrice = 10,
                    CategoryId = 1
                });
            productResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await SetActiveCompanyBillingPlanLimitsAsync(maxCustomers: 0);
            var customerResponse = await Client.PostAsJsonAsync(
                "/api/customers",
                new CreateCustomerCommand
                {
                    Name = $"Limited customer {Guid.NewGuid():N}"
                });
            customerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            await SetActiveCompanyBillingPlanLimitsAsync(maxInvoicesPerMonth: 0);
            var invoiceResponse = await Client.PostAsJsonAsync(
                "/api/sales-invoices",
                new CreateSalesInvoiceCommand
                {
                    InvoiceNumber = $"LIMIT-INV-{Guid.NewGuid():N}",
                    CustomerId = 1,
                    InvoiceDate = DateTime.UtcNow,
                    Items =
                    {
                        new CreateSalesInvoiceItemInput
                        {
                            ProductId = 1,
                            Quantity = 1,
                            SellingUnitPrice = 10,
                            TaxRate = 0
                        }
                    }
                });
            invoiceResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Invited_New_User_Should_Get_Free_Subscription_But_Not_Own_Company()
        {
            await AuthenticateAndCreateCompanyAsync();
            var invitedEmail = $"sub_invited_{Guid.NewGuid():N}@example.com";
            var inviteResponse = await Client.PostAsJsonAsync(
                "/api/company-invitations",
                new
                {
                    Email = invitedEmail,
                    Role = CompanyRoles.Viewer
                });
            inviteResponse.EnsureSuccessStatusCode();
            var invite = await inviteResponse.Content
                .ReadFromJsonAsync<CreateInvitationResponse>();
            invite.Should().NotBeNull();

            Client.DefaultRequestHeaders.Authorization = null;
            Client.DefaultRequestHeaders.Remove("X-Company-Id");
            var userName = $"sub_invited_{Guid.NewGuid():N}";
            var acceptResponse = await Client.PostAsJsonAsync(
                $"/api/company-invitations/{invite!.AcceptUrl.Split('/').Last()}/accept",
                new
                {
                    Mode = "newUser",
                    UserName = userName,
                    Password = "Password123",
                    ConfirmPassword = "Password123"
                });
            acceptResponse.EnsureSuccessStatusCode();

            await AuthenticateExistingUserAsync(userName, "Password123");
            var subscriptionResponse = await Client.GetAsync("/api/subscription/current");

            subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var subscription = await subscriptionResponse.Content
                .ReadFromJsonAsync<CurrentSubscriptionDto>();
            subscription.Should().NotBeNull();
            subscription!.Plan.Code.Should().Be(SubscriptionPlans.Free);
            subscription.Usage.OwnedCompanies.Should().Be(0);
        }

        private async Task AddDirectCompanyMemberAsync(int companyId)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var unique = Guid.NewGuid().ToString("N");
            var user = new ApplicationUser
            {
                UserName = $"subscription_member_{unique}",
                Email = $"subscription_member_{unique}@example.com"
            };

            var result = await userManager.CreateAsync(user, "Password123");
            result.Succeeded.Should().BeTrue();
            db.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = companyId,
                UserId = user.Id,
                Role = CompanyRoles.Viewer,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        private async Task AuthenticateExistingUserAsync(
            string userName,
            string password)
        {
            var loginResponse = await Client.PostAsJsonAsync(
                "/api/auth/login",
                new Application.Features.Auth.Login.Command
                {
                    UserName = userName,
                    Password = password
                });
            loginResponse.EnsureSuccessStatusCode();
            var login = await loginResponse.Content
                .ReadFromJsonAsync<Application.Features.Auth.Login.Response>();
            login.Should().NotBeNull();
            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    login!.AccessToken);
        }

        private sealed class CreateInvitationResponse
        {
            public string AcceptUrl { get; set; } = string.Empty;
        }
    }
}
