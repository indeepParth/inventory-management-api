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
            services.AddScoped<IActiveCompanyService, ActiveCompanyService>();
            services.AddScoped<IAuthorizationHandler, CompanyRoleAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.AdminOnly,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.AdminOnly));
                options.AddPolicy(AuthorizationPolicies.AdminOrManager,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.AdminOrManager));

                options.AddPolicy(AuthorizationPolicies.ReadProducts,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ReadProducts));
                options.AddPolicy(AuthorizationPolicies.ManageProducts,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageProducts));

                options.AddPolicy(AuthorizationPolicies.ReadCustomers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ReadCustomers));
                options.AddPolicy(AuthorizationPolicies.ManageCustomers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageCustomers));
                options.AddPolicy(AuthorizationPolicies.ViewCustomerStatements,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewCustomerStatements));

                options.AddPolicy(AuthorizationPolicies.ReadDrivers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ReadDrivers));
                options.AddPolicy(AuthorizationPolicies.ManageDrivers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageDrivers));

                options.AddPolicy(AuthorizationPolicies.ReadSuppliers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ReadSuppliers));
                options.AddPolicy(AuthorizationPolicies.ManageSuppliers,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageSuppliers));
                options.AddPolicy(AuthorizationPolicies.ViewSupplierStatements,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewSupplierStatements));

                options.AddPolicy(AuthorizationPolicies.ManagePurchases,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManagePurchases));
                options.AddPolicy(AuthorizationPolicies.ManageDeliveryChallans,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageDeliveryChallans));
                options.AddPolicy(AuthorizationPolicies.ManageSalesInvoices,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageSalesInvoices));
                options.AddPolicy(AuthorizationPolicies.ManageSupplierReturns,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageSupplierReturns));
                options.AddPolicy(AuthorizationPolicies.ManageCustomerReturns,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ManageCustomerReturns));

                options.AddPolicy(AuthorizationPolicies.ViewPayments,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewPayments));
                options.AddPolicy(AuthorizationPolicies.CreateCustomerReceipts,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.CreateCustomerReceipts));

                options.AddPolicy(AuthorizationPolicies.ViewStockMovements,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewStockMovements));
                options.AddPolicy(AuthorizationPolicies.RecordStockDamage,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.RecordStockDamage));

                options.AddPolicy(AuthorizationPolicies.ViewProductStockLedger,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewProductStockLedger));
                options.AddPolicy(AuthorizationPolicies.ViewSalesReports,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewSalesReports));
                options.AddPolicy(AuthorizationPolicies.ViewCostReports,
                    policy => AddCompanyRolePolicy(policy, AuthorizationPolicies.ViewCostReports));
            });

            return services;
        }

        private static void AddCompanyRolePolicy(
            AuthorizationPolicyBuilder policy,
            string permission)
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new CompanyRoleRequirement(
                    CompanyPermissions.GetRolesForPermission(permission)));
        }
    }
}
