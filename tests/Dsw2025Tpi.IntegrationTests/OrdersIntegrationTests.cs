using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class OrdersIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public OrdersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        // 1. ARRANGE
        var client = _factory.CreateAnonymousClient();
        var request = new OrderModel.Request(
            Guid.NewGuid(),
            "Av. Aconquija 1200",
            "Av. Aconquija 1200",
            new List<OrderItemModel.Request>
            {
                new(1, Guid.NewGuid(), "Teclado Mecánico", "Teclado switch rojo", 100m)
            }
        );

        // 2. ACT
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // 3. ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithValidDataAndStock_ReturnsCreatedAndPersistsOrderAndDecrementsStock()
    {
        // 1. ARRANGE
        var client = _factory.CreateClientWithRole(CustomWebApplicationFactory.CustomerRole);
        var customer = await _factory.SeedCustomerAsync("Javier Ferreyra");
        var product1 = await _factory.SeedProductAsync("SKU-TEC-01", "Teclado Mecánico", price: 100m, stock: 10);
        var product2 = await _factory.SeedProductAsync("SKU-MOU-02", "Mouse Gamer", price: 50m, stock: 8);

        var request = new OrderModel.Request(
            customer.Id,
            "Av. Aconquija 1200",
            "Av. Aconquija 1200",
            new List<OrderItemModel.Request>
            {
                new(2, product1.Id, product1.Name ?? "Teclado Mecánico", product1.Description ?? "Teclado", product1.CurrentUnitPrice ?? 100m),
                new(3, product2.Id, product2.Name ?? "Mouse Gamer", product2.Description ?? "Mouse", product2.CurrentUnitPrice ?? 50m)
            }
        );

        // 2. ACT
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // 3. ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var savedOrder = await _factory.ExecuteDbContextAsync(async db =>
            await db.Orders
                .Include(o => o.OrderItem)
                .FirstOrDefaultAsync(o => o.CustomerId == customer.Id));

        savedOrder.Should().NotBeNull();
        savedOrder!.OrderItem.Should().HaveCount(2);

        var updatedProduct1 = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product1.Id));

        var updatedProduct2 = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product2.Id));

        updatedProduct1.Should().NotBeNull();
        updatedProduct1!.StockQuantity.Should().Be(8);

        updatedProduct2.Should().NotBeNull();
        updatedProduct2!.StockQuantity.Should().Be(5);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequestAndDoesNotDecrementStock()
    {
        // 1. ARRANGE
        var client = _factory.CreateClientWithRole(CustomWebApplicationFactory.CustomerRole);
        var customer = await _factory.SeedCustomerAsync("Javier Ferreyra");
        var product = await _factory.SeedProductAsync("SKU-MON-03", "Monitor 27", price: 300m, stock: 2);

        var request = new OrderModel.Request(
            customer.Id,
            "San Martín 450",
            "San Martín 450",
            new List<OrderItemModel.Request>
            {
                new(5, product.Id, product.Name ?? "Monitor 27", product.Description ?? "Monitor", product.CurrentUnitPrice ?? 300m)
            }
        );

        // 2. ACT
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // 3. ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var ordersCount = await _factory.ExecuteDbContextAsync(async db =>
            await db.Orders.CountAsync(o => o.CustomerId == customer.Id));
        ordersCount.Should().Be(0);

        var unchangedProduct = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id));

        unchangedProduct.Should().NotBeNull();
        unchangedProduct!.StockQuantity.Should().Be(2);
    }
}
