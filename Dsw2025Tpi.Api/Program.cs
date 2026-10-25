using Dsw2025Tpi.Api.Configurations;
using Dsw2025Tpi.Api.Middleware;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsw2025Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuración JWT (sección "Jwt" de appsettings.json; en Azure se puede pisar con Jwt__Key)
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new InvalidOperationException("Falta la configuración 'Jwt:Key'.");
        var key = Encoding.UTF8.GetBytes(keyText);

        // La connection string se lee de la configuración:
        //  - Local: appsettings.Development.json (LocalDB)
        //  - Azure: cadena de conexión Dsw2025TpiDb de la Web App
        var connectionString = builder.Configuration.GetConnectionString("Dsw2025TpiDb")
            ?? throw new InvalidOperationException("Falta la connection string 'Dsw2025TpiDb'.");

        builder.Services.AddDbContext<Dsw2025TpiContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // La base Azure SQL free se pausa sola: reintenta mientras se despierta
                // en lugar de tirar la app (evita reinicios en loop y consumo de cuota).
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            };
        });

        // Identity: usuarios y roles guardados en la misma base
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
        .AddEntityFrameworkStores<Dsw2025TpiContext>()
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

        // CORS (front en desarrollo)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhost", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Servicios propios
        builder.Services.AddDomainServices(builder.Configuration);
        builder.Services.AddScoped<AuthManagementService>();
        builder.Services.AddSingleton<JwtTokenService>();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Swagger con botón "Authorize" para pegar el token JWT
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Dsw2025Tpi API",
                Version = "v1",
                Description = "Documentación y prueba de endpoints de la API Dsw2025Tpi"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Pegá solo el token devuelto por POST /api/auth/login (sin la palabra Bearer).",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
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
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // Al iniciar: aplica migraciones, crea los roles y el usuario admin de admins.json
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            services.GetRequiredService<Dsw2025TpiContext>().Database.Migrate();

            await SeedRolesAsync(services);
            await SeedAdminsAsync(services, Path.Combine(AppContext.BaseDirectory, "admins.json"));
        }

        // Swagger habilitado en TODOS los entornos (local y Azure)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dsw2025Tpi API v1");
            c.RoutePrefix = "swagger"; // https://<dominio>/swagger
        });

        app.UseCors("AllowLocalhost");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseStatusCodePages();

        app.MapControllers();
        app.MapHealthChecks("/healthcheck");

        app.Run();
    }

    static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { "admin", "customer", "seller" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Crea los usuarios admin listados en admins.json si todavía no existen.
    // Sin esto, recién desplegada la API no tendría ningún admin para iniciar sesión.
    static async Task SeedAdminsAsync(IServiceProvider services, string path)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (!File.Exists(path))
        {
            logger.LogWarning("No se encontró {Path}; no se crean usuarios admin.", path);
            return;
        }

        var admins = JsonSerializer.Deserialize<List<AdminSeed>>(
            await File.ReadAllTextAsync(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var admin in admins)
        {
            if (await userManager.FindByNameAsync(admin.Username) is not null)
                continue; // ya existe: no se toca

            var user = new IdentityUser { UserName = admin.Username, Email = admin.Email };
            var result = await userManager.CreateAsync(user, admin.Password);

            if (!result.Succeeded)
            {
                logger.LogError("No se pudo crear el admin {User}: {Errors}", admin.Username,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, "admin");
            logger.LogInformation("Usuario admin {User} creado.", admin.Username);
        }
    }

    public record AdminSeed(string Username, string Email, string Password);
}
