using System.Net;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class ProductsIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOkWithPaginatedJsonStructure()
    {
        // 1. ARRANGE: Sembrar al menos un producto activo en SQLite
        await _factory.SeedProductAsync("SKU-PUB-01", "Teclado Mecánico Público", price: 100m, stock: 10, isActive: true);

        var client = _factory.CreateAnonymousClient();

        // 2. ACT: Petición anónima (sin credenciales) a GET /api/products
        var response = await client.GetAsync("/api/products");

        // 3. ASSERT: Responde 200 OK y el cuerpo deserializa en la estructura con productItems y totalItems
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<ProductModel.ResponsePagination>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ProductItems.Should().NotBeNull();
        apiResponse.Data.ProductItems.Should().HaveCount(1);
        apiResponse.Data.TotalItems.Should().Be(1);
        apiResponse.Data.ProductItems.First().Sku.Should().Be("SKU-PUB-01");
    }

    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        // 1. ARRANGE: Sembrar 3 productos en SQLite
        await _factory.SeedProductAsync("SKU-MOU-01", "Mouse Gamer Óptico", price: 50m, stock: 15, isActive: true);
        await _factory.SeedProductAsync("SKU-TEC-02", "Teclado Mecánico RGB", price: 120m, stock: 8, isActive: true);
        await _factory.SeedProductAsync("SKU-MON-03", "Monitor 24 Pulgadas", price: 200m, stock: 5, isActive: true);

        var client = _factory.CreateAnonymousClient();

        // 2. ACT: Filtrar por search=mouse, pageNumber=1 y pageSize=10
        var response = await client.GetAsync("/api/products?search=mouse&pageNumber=1&pageSize=10");

        // 3. ASSERT: Devuelve solo los productos que cumplen el filtro
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<ProductModel.ResponsePagination>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.ProductItems.Should().HaveCount(1);
        apiResponse.Data.ProductItems.First().Sku.Should().Be("SKU-MOU-01");
        apiResponse.Data.ProductItems.First().Name.Should().Be("Mouse Gamer Óptico");
        apiResponse.Data.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetAuthProducts_AdminEndpoint_WhenCalledByAdmin_IncludesInactiveProducts()
    {
        // 1. ARRANGE: Sembrar un producto activo y uno inactivo en SQLite
        await _factory.SeedProductAsync("SKU-ACT-01", "Producto Activo", price: 100m, stock: 10, isActive: true);
        await _factory.SeedProductAsync("SKU-INA-02", "Producto Inactivo", price: 80m, stock: 0, isActive: false);

        var publicClient = _factory.CreateAnonymousClient();
        var adminClient = _factory.CreateClientWithRole(CustomWebApplicationFactory.AdminRole);

        // 2. ACT:
        // Catálogo público: no debe incluir inactivos
        var publicResponse = await publicClient.GetAsync("/api/products");
        // Endpoint administrativo con token de Admin: sí debe incluir inactivos
        var adminResponse = await adminClient.GetAsync("/api/products/admin");

        // 3. ASSERT:
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicApiResponse = await publicResponse.ReadApiResponseAsync<ProductModel.ResponsePagination>();
        publicApiResponse!.Data!.ProductItems.Should().HaveCount(1);
        publicApiResponse.Data.ProductItems.Should().OnlyContain(p => p.IsActive);
        publicApiResponse.Data.ProductItems.Should().NotContain(p => p.Sku == "SKU-INA-02");

        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminApiResponse = await adminResponse.ReadApiResponseAsync<ProductModel.ResponsePagination>();
        adminApiResponse!.Data!.ProductItems.Should().HaveCount(2);
        adminApiResponse.Data.TotalItems.Should().Be(2);
        adminApiResponse.Data.ProductItems.Should().Contain(p => p.Sku == "SKU-INA-02" && !p.IsActive);
        adminApiResponse.Data.ProductItems.Should().Contain(p => p.Sku == "SKU-ACT-01" && p.IsActive);
    }
}
