using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.API.Services;
using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.OpenApi.Models;

namespace InventoryManagement.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApi(this IServiceCollection services)
        {
            services.AddControllers();
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

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.CanDeleteProducts,
                    policy =>
                    {
                        policy.RequireClaim("Permission", AuthorizationPolicies.CanDeleteProducts);
                    });
                options.AddPolicy(AuthorizationPolicies.CanManageUsers,
                    policy =>
                    {
                        policy.RequireClaim("Permission", AuthorizationPolicies.CanManageUsers);
                    });
            });

            return services;
        }
    }
}