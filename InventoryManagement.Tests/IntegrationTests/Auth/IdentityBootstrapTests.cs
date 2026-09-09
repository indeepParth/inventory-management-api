using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Options;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Tests.IntegrationTests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Tests.IntegrationTests.Auth
{
    public class IdentityBootstrapTests
    {
        [Fact]
        public async Task BootstrapAsync_Should_Create_First_Owner_Company_When_Production_Config_Is_Enabled()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions());

            await context.BootstrapService.BootstrapAsync();

            var admin = await context.UserManager.FindByNameAsync("admin");
            admin.Should().NotBeNull();

            var company = context.DbContext.Companies.SingleOrDefault();
            company.Should().NotBeNull();
            company!.Name.Should().Be("admin's Company");

            var membership = context.DbContext.CompanyUsers.SingleOrDefault(x =>
                x.CompanyId == company.Id &&
                x.UserId == admin!.Id);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(CompanyRoles.Owner);

            var globalRoles = await context.UserManager.GetRolesAsync(admin!);
            globalRoles.Should().BeEmpty();
        }

        [Fact]
        public async Task BootstrapAsync_Should_Be_Safe_On_Repeated_Startup()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions());

            await context.BootstrapService.BootstrapAsync();
            await context.BootstrapService.BootstrapAsync();

            context.DbContext.Users.Count().Should().Be(1);
            context.DbContext.Companies.Count().Should().Be(1);
            context.DbContext.CompanyUsers
                .Count(x => x.Role == CompanyRoles.Owner)
                .Should()
                .Be(1);
        }

        [Fact]
        public async Task BootstrapAsync_Should_Not_Create_Admin_Outside_Production_By_Default()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions(),
                Environments.Development);

            await context.BootstrapService.BootstrapAsync();

            var admin = await context.UserManager.FindByNameAsync("admin");
            admin.Should().BeNull();
        }

        [Fact]
        public async Task BootstrapAsync_Should_Create_Admin_Outside_Production_When_Explicitly_Allowed()
        {
            var options = ValidAdminOptions();
            options.AllowOutsideProduction = true;

            await using var context = await CreateBootstrapContextAsync(
                options,
                Environments.Development);

            await context.BootstrapService.BootstrapAsync();

            var admin = await context.UserManager.FindByNameAsync("admin");
            admin.Should().NotBeNull();

            context.DbContext.CompanyUsers
                .Single(x => x.UserId == admin!.Id)
                .Role
                .Should()
                .Be(CompanyRoles.Owner);
        }

        [Fact]
        public async Task BootstrapAsync_Should_Fail_When_Enabled_Admin_Config_Is_Missing()
        {
            var options = ValidAdminOptions();
            options.Password = string.Empty;
            await using var context = await CreateBootstrapContextAsync(options);

            var act = async () => await context.BootstrapService.BootstrapAsync();

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*Bootstrap:Admin:Password*");
        }

        [Fact]
        public async Task BootstrapAsync_Should_Not_Overwrite_Existing_User()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions());

            var existingUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com"
            };

            var createResult = await context.UserManager.CreateAsync(
                existingUser,
                "ExistingPassword123");
            createResult.Succeeded.Should().BeTrue();

            var act = async () => await context.BootstrapService.BootstrapAsync();

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*Existing users are not modified*");

            context.DbContext.CompanyUsers
                .Any(x => x.UserId == existingUser.Id)
                .Should()
                .BeFalse();
        }

        private static async Task<BootstrapTestContext> CreateBootstrapContextAsync(
            AdminBootstrapOptions adminOptions,
            string environmentName = "Production")
        {
            var database = new PostgresTestDatabase();
            await database.CreateAsync();

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(database.ConnectionString);
            });

            services.AddSingleton<IHostEnvironment>(
                new TestHostEnvironment(environmentName));

            services.AddSingleton(
                Options.Create(adminOptions));

            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IdentityBootstrapService>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.MigrateAsync();

            return new BootstrapTestContext(
                database,
                scope,
                dbContext,
                scope.ServiceProvider.GetRequiredService<IdentityBootstrapService>(),
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
        }

        private static AdminBootstrapOptions ValidAdminOptions()
        {
            return new AdminBootstrapOptions
            {
                Enabled = true,
                UserName = "admin",
                Email = "admin@example.com",
                Password = "AdminPassword123"
            };
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "InventoryManagement.Tests";
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private sealed class BootstrapTestContext : IAsyncDisposable
        {
            private readonly PostgresTestDatabase _database;
            private readonly IServiceScope _scope;

            public BootstrapTestContext(
                PostgresTestDatabase database,
                IServiceScope scope,
                ApplicationDbContext dbContext,
                IdentityBootstrapService bootstrapService,
                UserManager<ApplicationUser> userManager)
            {
                _database = database;
                _scope = scope;
                DbContext = dbContext;
                BootstrapService = bootstrapService;
                UserManager = userManager;
            }

            public ApplicationDbContext DbContext { get; }
            public IdentityBootstrapService BootstrapService { get; }
            public UserManager<ApplicationUser> UserManager { get; }

            public async ValueTask DisposeAsync()
            {
                _scope.Dispose();
                await _database.DisposeAsync();
            }
        }
    }
}
