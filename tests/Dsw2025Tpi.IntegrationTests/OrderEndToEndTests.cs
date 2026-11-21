using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class OrderEndToEndTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAndListOrder_EndToEndFlow_PersistsOrderAndUpdatesStock()
    {
        // 1. ARRANGE
        var customerClient = _factory.CreateClientWithRole(CustomWebApplicationFactory.CustomerRole);
        var adminClient = _factory.CreateClientWithRole(CustomWebApplicationFactory.AdminRole);

        var customer = await _factory.SeedCustomerAsync("Cliente Flujo E2E");
        var product = await _factory.SeedProductAsync("SKU-E2E-01", "Teclado Mecánico E2E", price: 120m, stock: 10);

        var createRequest = new OrderModel.Request(
            customer.Id,
            "Av. Aconquija 1200",
            "Av. Aconquija 1200",
            new List<OrderItemModel.Request>
            {
                new(2, product.Id, product.Name ?? "Teclado Mecánico E2E", product.Description ?? "Teclado", product.CurrentUnitPrice ?? 120m)
            }
        );

        // 2. ACT - Paso 1: Crear orden (POST /api/orders)
        var createResponse = await customerClient.PostAsJsonAsync("/api/orders", createRequest);

        // ASSERT INTERMEDIO: Verificar creación y extraer OrderId
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, because: $"POST /api/orders devolvió: {createBody}");

        using var doc = JsonDocument.Parse(createBody);
        Guid? orderId = TryExtractOrderId(doc.RootElement);
        orderId.Should().NotBeNull("la respuesta de creación de orden debe incluir el identificador de la orden");

        // ACT - Paso 2: Consultar órdenes desde la API (GET /api/orders)
        var getResponse = await adminClient.GetAsync("/api/orders");
        var getBody = await getResponse.Content.ReadAsStringAsync();

        // 3. ASSERT FINAL: Validar respuesta de consulta y estado persistido en SQLite
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, because: $"GET /api/orders devolvió: {getBody}");
        getBody.Should().Contain(orderId!.Value.ToString());

        var savedOrder = await _factory.ExecuteDbContextAsync(async db =>
            await db.Orders
                .Include(o => o.OrderItem)
                .FirstOrDefaultAsync(o => o.Id == orderId.Value));

        savedOrder.Should().NotBeNull();
        savedOrder!.CustomerId.Should().Be(customer.Id);
        savedOrder.OrderItem.Should().HaveCount(1);

        var updatedProduct = await _factory.ExecuteDbContextAsync(async db =>
            await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id));

        updatedProduct.Should().NotBeNull();
        updatedProduct!.StockQuantity.Should().Be(8);
    }

    private static Guid? TryExtractOrderId(JsonElement root)
    {
        string[] candidateKeys = ["orderId", "OrderId", "id", "Id"];

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in candidateKeys)
            {
                if (root.TryGetProperty(key, out var prop) && prop.TryGetGuid(out var guid))
                    return guid;
            }

            if (root.TryGetProperty("data", out var dataProp) || root.TryGetProperty("Data", out dataProp))
            {
                return TryExtractOrderId(dataProp);
            }
        }

        return null;
    }
}