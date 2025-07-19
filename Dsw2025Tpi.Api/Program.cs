
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

namespace Dsw2025Tpi.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddDbContext<Dsw2025TpiContext>(options => {
        options.UseSqlServer("Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = Dsw2025TpiDb; Integrated Security = True;");
     options.UseSeeding((c, t) =>
     {
         ((Dsw2025TpiContext)c).Seedwork<Customer>("Source\\customers.json");
    });
        });
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      });

        builder.Services.AddTransient<IRepository, EfRepository>();
        builder.Services.AddScoped<ProductsManagmentService>();
        builder.Services.AddScoped<OrdersManagmentService>();
        builder.Services.AddScoped<CustomerManagmentService>();
        builder.Services.AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
        });
   
     

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        
        app.MapHealthChecks("/healthcheck");

        app.Run();
    }
}
