using System.Net;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.IntegrationTests;

/// <summary>
/// Pruebas de verificación de la infraestructura de integración.
/// No prueban reglas de negocio: confirman que la API se levanta en memoria,
/// que usa SQLite (y no SQL Server) y que los tokens de prueba son aceptados.
/// </summary>
[Collection(IntegrationTestsCollection.Name)]
public class SetupVerificationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SetupVerificationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_StartsInMemory_AndHealthCheckReturnsOk()
    {
        // Arrange
        var client = _factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/healthcheck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Database_UsesSqliteInMemory_InsteadOfSqlServer()
    {
        // Act
        var provider = await _factory.ExecuteDbContextAsync(db => Task.FromResult(db.Database.ProviderName));

        // Assert
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", provider);
    }

    [Fact]
    public async Task Database_CanSeedAndReadData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var sku = $"SETUP-{Guid.NewGuid():N}"[..20];

        // Act
        await _factory.SeedProductAsync(sku, "Producto de verificación");
        var exists = await _factory.ExecuteDbContextAsync(db => db.Products.AnyAsync(p => p.Sku == sku));

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AdminToken_IsAcceptedByProtectedEndpoint()
    {
        // Arrange: GET /api/orders requiere [Authorize(Roles = "admin")]
        var client = _factory.CreateClientWithRole(CustomWebApplicationFactory.AdminRole);

        // Act
        var response = await client.GetAsync("/api/orders");

        // Assert: el token generado para pruebas es válido para la API
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
