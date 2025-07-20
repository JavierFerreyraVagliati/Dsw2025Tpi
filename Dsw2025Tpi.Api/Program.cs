using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Helpers;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Api.Configurations;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Cargar configuración JWT
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]);


  

        builder.Services.AddDbContext<AuthenticateContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Dsw2025TpiDb"));
        });

        // Agregar Identity
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireNonAlphanumeric = false,
                RequireUppercase = true,
                RequireLowercase = true,
            };
        })
        .AddEntityFrameworkStores<AuthenticateContext>()
        .AddDefaultTokenProviders();

        // Autenticación JWT
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidAudience = jwtConfig["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("PermitirFrontend", policy =>
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        // Servicios propios
        builder.Services.AddTransient<IRepository, EfRepository>();
        builder.Services.AddScoped<ProductsManagmentService>();
        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddDomainServices(builder.Configuration);

        // Controladores
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Swagger con JWT
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Dsw2025Tpi API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Ingrese el token JWT con el prefijo 'Bearer '",
                Type = SecuritySchemeType.ApiKey
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
        });

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // Middlewares
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("PermitirFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/healthcheck");

        app.Run();
    }
}
