using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class ProductsCreationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsCreationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddProduct_WhenUnauthenticatedOrNotAdmin_ReturnsUnauthorizedOrForbidden()
    {
        // 1. ARRANGE
        var anonymousClient = _factory.CreateAnonymousClient();
        var customerClient = _factory.CreateClientWithRole(CustomWebApplicationFactory.CustomerRole);

        var request = new ProductModel.RequestProduct(
            "SKU-TEC-01",
            "INT-001",
            "Teclado Mecánico",
            "Teclado switch rojo",
            100m,
            10
        );

        // 2. ACT
        var anonymousResponse = await anonymousClient.PostAsJsonAsync("/api/products", request);
        var customerResponse = await customerClient.PostAsJsonAsync("/api/products", request);

        // 3. ASSERT
        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var productsCount = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.CountAsync(p => p.Sku == "SKU-TEC-01"));
        productsCount.Should().Be(0);
    }

    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        // 1. ARRANGE
        var client = _factory.CreateClientWithRole(CustomWebApplicationFactory.AdminRole);

        var request = new ProductModel.RequestProduct(
            "SKU-MOU-02",
            "INT-002",
            "Mouse Gamer",
            "Mouse inalámbrico",
            50m,
            8
        );

        // 2. ACT
        var response = await client.PostAsJsonAsync("/api/products", request);

        // 3. ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.ReadApiResponseAsync<ProductModel.ResponseProduct>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Sku.Should().Be("SKU-MOU-02");

        var savedProduct = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == "SKU-MOU-02"));

        savedProduct.Should().NotBeNull();
        savedProduct!.Id.Should().Be(body.Data.Id);
        savedProduct.InternalCode.Should().Be("INT-002");
        savedProduct.Name.Should().Be("Mouse Gamer");
        savedProduct.Description.Should().Be("Mouse inalámbrico");
        savedProduct.CurrentUnitPrice.Should().Be(50m);
        savedProduct.StockQuantity.Should().Be(8);
        savedProduct.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExistsInDatabase_ReturnsConflictStatusCode()
    {
        // 1. ARRANGE
        var client = _factory.CreateClientWithRole(CustomWebApplicationFactory.AdminRole);
        await _factory.SeedProductAsync("SKU-MON-03", "Monitor 27", price: 300m, stock: 5);

        var request = new ProductModel.RequestProduct(
            "SKU-MON-03",
            "INT-999",
            "Monitor 27 Curvo",
            "Otro monitor con el mismo SKU",
            350m,
            3
        );

        // 2. ACT
        var response = await client.PostAsJsonAsync("/api/products", request);

        // 3. ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var productsWithSku = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().Where(p => p.Sku == "SKU-MON-03").ToListAsync());

        productsWithSku.Should().HaveCount(1);
        productsWithSku[0].Name.Should().Be("Monitor 27");
    }
}
