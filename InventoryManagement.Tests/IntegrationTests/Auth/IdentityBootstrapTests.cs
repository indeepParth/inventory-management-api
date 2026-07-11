using FluentAssertions;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Options;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
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
        public async Task BootstrapAsync_Should_Create_Roles_And_First_Admin_When_Production_Config_Is_Enabled()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions());

            await context.BootstrapService.BootstrapAsync();

            foreach (var role in ApplicationRoles.All)
            {
                var exists = await context.RoleManager.RoleExistsAsync(role);
                exists.Should().BeTrue();
            }

            var admin = await context.UserManager.FindByNameAsync("admin");
            admin.Should().NotBeNull();

            var isAdmin = await context.UserManager.IsInRoleAsync(
                admin!,
                ApplicationRoles.Admin);
            isAdmin.Should().BeTrue();
        }

        [Fact]
        public async Task BootstrapAsync_Should_Be_Safe_On_Repeated_Startup()
        {
            await using var context = await CreateBootstrapContextAsync(
                ValidAdminOptions());

            await context.BootstrapService.BootstrapAsync();
            await context.BootstrapService.BootstrapAsync();

            var admins = await context.UserManager.GetUsersInRoleAsync(
                ApplicationRoles.Admin);

            admins.Should().ContainSingle();
            context.DbContext.Roles.Count().Should().Be(ApplicationRoles.All.Length);
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

            var isAdmin = await context.UserManager.IsInRoleAsync(
                admin!,
                ApplicationRoles.Admin);
            isAdmin.Should().BeTrue();
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

            var isAdmin = await context.UserManager.IsInRoleAsync(
                existingUser,
                ApplicationRoles.Admin);
            isAdmin.Should().BeFalse();
        }

        private static async Task<BootstrapTestContext> CreateBootstrapContextAsync(
            AdminBootstrapOptions adminOptions,
            string environmentName = "Production")
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connection);
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

            await dbContext.Database.EnsureCreatedAsync();

            return new BootstrapTestContext(
                connection,
                scope,
                dbContext,
                scope.ServiceProvider.GetRequiredService<IdentityBootstrapService>(),
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
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
            private readonly SqliteConnection _connection;
            private readonly IServiceScope _scope;

            public BootstrapTestContext(
                SqliteConnection connection,
                IServiceScope scope,
                ApplicationDbContext dbContext,
                IdentityBootstrapService bootstrapService,
                UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager)
            {
                _connection = connection;
                _scope = scope;
                DbContext = dbContext;
                BootstrapService = bootstrapService;
                UserManager = userManager;
                RoleManager = roleManager;
            }

            public ApplicationDbContext DbContext { get; }
            public IdentityBootstrapService BootstrapService { get; }
            public UserManager<ApplicationUser> UserManager { get; }
            public RoleManager<IdentityRole> RoleManager { get; }

            public async ValueTask DisposeAsync()
            {
                _scope.Dispose();
                await _connection.DisposeAsync();
            }
        }
    }
}
