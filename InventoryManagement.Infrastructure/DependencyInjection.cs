using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Options;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace InventoryManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(Options =>
            {
                Options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IDriverRepository, DriverRepository>();
            services.AddScoped<IStockMovementRepository, StockMovementRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IDeliveryChallanRepository, DeliveryChallanRepository>();
            services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
            services.AddScoped<ICustomerReturnRepository, CustomerReturnRepository>();
            services.AddScoped<ISupplierReturnRepository, SupplierReturnRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPartyStatementRepository, PartyStatementRepository>();
            services.AddScoped<IGrossProfitReportRepository, GrossProfitReportRepository>();
            services.AddScoped<IDocumentNumberService, DocumentNumberService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IdentityBootstrapService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddOptions<AdminBootstrapOptions>()
                .Bind(configuration.GetSection(AdminBootstrapOptions.SectionName));

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
                {
                    var jwtOptions = jwtOptionsAccessor.Value;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.Key))
                    };
                });

            services.AddIdentityCore<ApplicationUser>(option =>
                {
                    option.User.RequireUniqueEmail = true;
                    option.Password.RequiredLength = 6;
                    option.Password.RequireDigit = true;
                    option.Password.RequireLowercase = false;
                    option.Password.RequireUppercase = false;
                    option.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}
