using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
            services.AddScoped<IStockMovementRepository, StockMovementRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IDeliveryChallanRepository, DeliveryChallanRepository>();
            services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
            services.AddScoped<ICustomerReturnRepository, CustomerReturnRepository>();
            services.AddScoped<ISupplierReturnRepository, SupplierReturnRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPartyStatementRepository, PartyStatementRepository>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
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
