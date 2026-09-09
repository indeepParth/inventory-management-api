using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.API.Authorization;
using InventoryManagement.API.Configuration;
using InventoryManagement.API.HealthChecks;
using InventoryManagement.API.Services;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Options;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace InventoryManagement.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApi(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<AuthenticationOptions>()
                .BindConfiguration(AuthenticationOptions.SectionName);

            services.Configure<ForwardedHeadersOptions>(options =>
                ForwardedHeadersConfiguration.Configure(options, configuration));

            services.Configure<ApiRateLimitOptions>(
                configuration.GetSection(ApiRateLimitOptions.SectionName));
            services.AddRateLimiter(_ => { });
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<ApiRateLimitOptions>>(
                    (options, rateLimitOptions) =>
                        RateLimitingConfiguration.Configure(
                            options,
                            rateLimitOptions.Value));

            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyNames.Frontend, policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .ToArray())
                        .WithMethods(
                            HttpMethods.Get,
                            HttpMethods.Post,
                            HttpMethods.Put,
                            HttpMethods.Patch,
                            HttpMethods.Delete)
                        .WithHeaders(
                            "Authorization",
                            "Content-Type",
                            "X-Company-Id");
                });
            });

            services.AddControllers();
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>(
                    "database",
                    tags: ["ready"]);
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT Token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Use the full namespace + class name to prevent schema ID conflicts
                options.CustomSchemaIds(type => type.FullName);
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAuthorizationHandler, CompanyRoleAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.AdminOnly,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Owner));
                options.AddPolicy(AuthorizationPolicies.AdminOrManager,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));

                options.AddPolicy(AuthorizationPolicies.ReadProducts,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.ManageProducts,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));

                options.AddPolicy(AuthorizationPolicies.ReadCustomers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));
                options.AddPolicy(AuthorizationPolicies.ManageCustomers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));
                options.AddPolicy(AuthorizationPolicies.ViewCustomerStatements,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));

                options.AddPolicy(AuthorizationPolicies.ReadDrivers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));
                options.AddPolicy(AuthorizationPolicies.ManageDrivers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));

                options.AddPolicy(AuthorizationPolicies.ReadSuppliers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.ManageSuppliers,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));
                options.AddPolicy(AuthorizationPolicies.ViewSupplierStatements,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));

                options.AddPolicy(AuthorizationPolicies.ManagePurchases,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.ManageDeliveryChallans,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));
                options.AddPolicy(AuthorizationPolicies.ManageSalesInvoices,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));
                options.AddPolicy(AuthorizationPolicies.ManageSupplierReturns,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.ManageCustomerReturns,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));

                options.AddPolicy(AuthorizationPolicies.ViewPayments,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));
                options.AddPolicy(AuthorizationPolicies.CreateCustomerReceipts,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));

                options.AddPolicy(AuthorizationPolicies.ViewStockMovements,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.RecordStockDamage,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));

                options.AddPolicy(AuthorizationPolicies.ViewProductStockLedger,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Inventory));
                options.AddPolicy(AuthorizationPolicies.ViewSalesReports,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager,
                        CompanyRoles.Sales));
                options.AddPolicy(AuthorizationPolicies.ViewCostReports,
                    policy => AddCompanyRolePolicy(
                        policy,
                        CompanyRoles.Manager));
            });

            return services;
        }

        private static void AddCompanyRolePolicy(
            AuthorizationPolicyBuilder policy,
            params string[] allowedRoles)
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new CompanyRoleRequirement(allowedRoles));
        }
    }
}
