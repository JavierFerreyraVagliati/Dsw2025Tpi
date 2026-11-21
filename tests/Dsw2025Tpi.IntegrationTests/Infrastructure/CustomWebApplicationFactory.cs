using System.Net.Http.Headers;
using Dsw2025Tpi.Api;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta la API completa en memoria (controllers, middlewares, autenticación JWT y EF Core)
/// reemplazando SQL Server por una base SQLite en memoria.
///
/// - Entorno "Testing": Program.cs crea el esquema con EnsureCreated en lugar de
///   aplicar las migraciones de SQL Server.
/// - La base vive mientras la conexión "_keepAliveConnection" esté abierta,
///   es decir, durante toda la sesión de pruebas de la colección.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminRole = "admin";
    public const string CustomerRole = "customer";

    // Nombre único: cada fábrica tiene su propia base en memoria compartida
    private readonly string _connectionString =
        $"DataSource=Dsw2025TpiTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // Una base SQLite en memoria se borra cuando se cierra la última conexión.
    // Esta conexión queda abierta para que la base viva durante todas las pruebas.
    private readonly SqliteConnection _keepAliveConnection;

    public CustomWebApplicationFactory()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Quitar la configuración de SQL Server registrada en Program.cs.
            // En EF Core 9 hay que quitar también IDbContextOptionsConfiguration,
            // si no, quedarían dos proveedores configurados (SqlServer + Sqlite).
            services.RemoveAll<DbContextOptions<Dsw2025TpiContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<Dsw2025TpiContext>>();

            services.AddDbContext<Dsw2025TpiContext>(options =>
                options.UseSqlite(_connectionString));
        });
    }

    // ---------------------------------------------------------------
    // Clientes HTTP
    // ---------------------------------------------------------------

    /// <summary>Cliente HTTP sin token (usuario anónimo).</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>
    /// Cliente HTTP con un token JWT válido para el rol indicado ("admin", "customer", ...).
    /// El token se genera con el mismo JwtTokenService que usa la API.
    /// </summary>
    public HttpClient CreateClientWithRole(string role, string username = "integration-test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(role, username));
        return client;
    }

    public string GenerateToken(string role, string username = "integration-test-user")
    {
        var jwt = Services.GetRequiredService<JwtTokenService>();
        return jwt.GenerateToken(username, role);
    }

    // ---------------------------------------------------------------
    // Acceso directo a la base (para sembrar datos y verificar resultados)
    // ---------------------------------------------------------------

    /// <summary>Ejecuta código con un DbContext nuevo sobre la base SQLite de pruebas.</summary>
    public async Task ExecuteDbContextAsync(Func<Dsw2025TpiContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        await action(db);
    }

    /// <summary>Ejecuta código con un DbContext nuevo y devuelve un resultado.</summary>
    public async Task<T> ExecuteDbContextAsync<T>(Func<Dsw2025TpiContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        return await action(db);
    }

    /// <summary>
    /// Borra los datos de negocio (órdenes, productos y clientes) para que cada prueba
    /// arranque limpia. Los usuarios y roles de Identity se conservan.
    /// </summary>
    public Task ResetDatabaseAsync() => ExecuteDbContextAsync(async db =>
    {
        db.OrderItems.RemoveRange(db.OrderItems);
        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();

        db.Products.RemoveRange(db.Products);
        db.Customers.RemoveRange(db.Customers);
        await db.SaveChangesAsync();
    });

    /// <summary>
    /// Crea un cliente válido en la base. Customer tiene una clave foránea a la tabla
    /// de usuarios, así que primero se crea el usuario de Identity.
    /// </summary>
    public async Task<Customer> SeedCustomerAsync(string name = "Cliente de prueba")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();

        var username = $"cliente-{Guid.NewGuid():N}";
        var user = new IdentityUser { UserName = username, Email = $"{username}@test.local" };
        var result = await userManager.CreateAsync(user, "Password123");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var customer = new Customer { UserId = user.Id, Name = name, Email = user.Email };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    /// <summary>Crea un producto en la base y lo devuelve.</summary>
    public Task<Product> SeedProductAsync(string sku, string name, decimal price = 100m, int stock = 10, bool isActive = true)
        => ExecuteDbContextAsync(async db =>
        {
            var product = new Product(sku, $"INT-{sku}", name, $"Descripción de {name}", price, stock)
            {
                IsActive = isActive
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product;
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAliveConnection.Dispose();
    }
}
