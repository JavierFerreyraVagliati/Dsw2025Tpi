
using Microsoft.AspNetCore.DataProtection.Repositories;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
    options.UseSqlServer("Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = Dsw2025TpiDb; Integrated Security = True;"));
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddTransient<IRepository, EfRepository>();

        builder.Services.AddScoped<ProductsManagmentService>();
        builder.Services.AddScoped<OrdersManagmentService>();

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
