using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Data.Helpers;
using Microsoft.EntityFrameworkCore;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Api.Configurations;

public static class DomainServicesConfigurationExtension
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Dsw2025TpiDb"));
            options.UseSeeding((c, t) =>
            {
                ((Dsw2025TpiContext)c).Seedwork<Product>("Source\\products.json");
                ((Dsw2025TpiContext)c).Seedwork<Customer>("Source\\customers.json");
            });
        });

        services.AddScoped<IRepository, EfRepository>();
        services.AddScoped<ProductsManagmentService>();
        services.AddScoped<OrdersManagmentService>();
        services.AddScoped<CustomerManagmentService>();

        return services;
    }
}
