
using Microsoft.AspNetCore.DataProtection.Repositories;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Data.Helpers;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer("Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = Dsw2025TpiDb; Integrated Security = True;");
            options.UseSeeding((c, t) =>
            {
                ((Dsw2025TpiContext)c).Seedwork<Customer>("Source\\customers.json");
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();

        // Configuración de Swagger/OpenAPI unificada
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Dsw2025Tpi API",
                Version = "v1",
                Description = "Documentación y prueba de endpoints de la API Dsw2025Tpi"
            });
            c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
        });

        builder.Services.AddHealthChecks();

        builder.Services.AddTransient<IRepository, EfRepository>();
        builder.Services.AddScoped<ProductsManagmentService>();
        builder.Services.AddScoped<OrdersManagmentService>();
        builder.Services.AddScoped<CustomerManagmentService>();

        var app = builder.Build();

        // Habilitamos Swagger para TODOS los entornos (Local y Azure Production)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dsw2025Tpi API v1");
            c.RoutePrefix = "swagger"; // Accesible en: https://<tu-app>.azurewebsites.net/swagger
        });

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/healthcheck");

        app.Run();
    }
}