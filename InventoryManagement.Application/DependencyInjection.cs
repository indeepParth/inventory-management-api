using FluentValidation;
using InventoryManagement.Application.Common.Behaviors;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Add MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            // Services
            services.AddScoped<ITokenService, TokenService>();

            // AutoMapper
            // services.AddAutoMapper(typeof(ProductProfile));

            // FluentValidation
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(BusinessTransitionLoggingBehavior<,>));


            return services;
        }
    }
}
