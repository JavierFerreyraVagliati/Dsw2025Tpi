using System.Linq.Expressions;
using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dsw2025Tpi.UnitTests;

public class ProductsManagmentServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ProductsManagmentService _service;

    public ProductsManagmentServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ProductsManagmentService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetProducts_WhenIncludeInactiveIsFalse_ReturnsOnlyActiveProducts()
    {
        // 1. ARRANGE: Lista mixta con productos activos e inactivos
        var products = new List<Product>
        {
            new Product("SKU-01", "INT-01", "Mouse Gamer", "Descripción", 100m, 10) { IsActive = true },
            new Product("SKU-02", "INT-02", "Teclado Mecánico", "Descripción", 150m, 5) { IsActive = false },
            new Product("SKU-03", "INT-03", "Monitor 24", "Descripción", 250m, 8) { IsActive = true }
        };

        _repositoryMock
            .Setup(r => r.GetFiltered<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[]? _) =>
                products.Where(predicate.Compile()).ToList());

        var filter = new ProductModel.FilterProduct(Status: null, Search: null, PageNumber: 1, PageSize: 10);

        // 2. ACT: isAdmin = false representa la consulta pública (excluye inactivos)
        var response = await _service.GetProducts(filter, isAdmin: false);

        // 3. ASSERT: El resultado solo debe incluir productos activos
        response.Should().NotBeNull();
        response!.ProductItems.Should().HaveCount(2);
        response.ProductItems.Should().OnlyContain(p => p.IsActive);
        response.ProductItems.Should().NotContain(p => p.Sku == "SKU-02");
        response.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task GetProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        // 1. ARRANGE: 15 productos con SKU ordenables
        var products = Enumerable.Range(1, 15)
            .Select(i => new Product($"SKU-{i:D2}", $"INT-{i:D2}", $"Producto {i:D2}", "Descripción", 100m, 10) { IsActive = true })
            .ToList();

        _repositoryMock
            .Setup(r => r.GetFiltered<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[]? _) =>
                products.Where(predicate.Compile()).ToList());

        // Solicitamos página 2 con tamaño de página 5
        var filter = new ProductModel.FilterProduct(Status: null, Search: null, PageNumber: 2, PageSize: 5);

        // 2. ACT
        var response = await _service.GetProducts(filter, isAdmin: true);

        // 3. ASSERT: La página 2 debe devolver exactamente 5 ítems y el total reportado debe ser 15
        response.Should().NotBeNull();
        response!.ProductItems.Should().HaveCount(5);
        response.TotalItems.Should().Be(15);

        // Los ítems devueltos deben corresponder a los elementos 6 al 10 (ordenados por SKU)
        var expectedSkus = Enumerable.Range(6, 5).Select(i => $"SKU-{i:D2}").ToList();
        response.ProductItems.Select(p => p.Sku).Should().ContainInOrder(expectedSkus);
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersByNameSkuOrInternalCode()
    {
        // 1. ARRANGE: Productos que coinciden por Nombre, SKU o Código Interno, y otros que no
        var products = new List<Product>
        {
            new Product("SKU-01", "INT-01", "Mouse Gamer RGB", "Óptico", 100m, 10) { IsActive = true },
            new Product("MOUSE-WIRELESS", "INT-02", "Periférico", "Inalámbrico", 120m, 8) { IsActive = true },
            new Product("SKU-03", "INT-MOUSE-PRO", "Accesorio", "Ergonómico", 90m, 15) { IsActive = true },
            new Product("SKU-04", "INT-04", "Teclado Mecánico", "Switch Red", 150m, 5) { IsActive = true },
            new Product("SKU-05", "INT-05", "Monitor 27 IPS", "144Hz", 300m, 4) { IsActive = true }
        };

        _repositoryMock
            .Setup(r => r.GetFiltered<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[]? _) =>
                products.Where(predicate.Compile()).ToList());

        // Buscamos "mouse" en minúsculas (debe coincidir sin distinguir mayúsculas/minúsculas)
        var filter = new ProductModel.FilterProduct(Status: null, Search: "mouse", PageNumber: 1, PageSize: 10);

        // 2. ACT
        var response = await _service.GetProducts(filter, isAdmin: true);

        // 3. ASSERT: Coincide por Nombre ("Mouse Gamer RGB"), SKU ("MOUSE-WIRELESS") y Código Interno ("INT-MOUSE-PRO")
        response.Should().NotBeNull();
        response!.ProductItems.Should().HaveCount(3);
        response.TotalItems.Should().Be(3);
        response.ProductItems.Select(p => p.Sku).Should().BeEquivalentTo(new[] { "SKU-01", "MOUSE-WIRELESS", "SKU-03" });
    }

    [Fact]
    public async Task AddProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
    {
        // 1. ARRANGE
        var request = new ProductModel.RequestProduct(
            "SKU-TEC-01",
            "INT-001",
            "Teclado Mecánico",
            "Teclado switch rojo",
            100m,
            10
        );

        Product? capturedProduct = null;

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product)null!);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .Callback<Product>(p => capturedProduct = p)
            .ReturnsAsync((Product p) => p);

        // 2. ACT
        var response = await _service.AddProduct(request);

        // 3. ASSERT
        response.Should().NotBeNull();
        response.Sku.Should().Be("SKU-TEC-01");
        response.Name.Should().Be("Teclado Mecánico");
        response.CurrentUnitPrice.Should().Be(100m);
        response.StockQuantity.Should().Be(10);
        response.IsActive.Should().BeTrue();

        capturedProduct.Should().NotBeNull();
        response.Id.Should().Be(capturedProduct!.Id);

        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException()
    {
        // 1. ARRANGE
        var existingProduct = new Product("SKU-MOU-02", "INT-002", "Mouse Gamer", "Mouse inalámbrico", 50m, 8);

        var request = new ProductModel.RequestProduct(
            "SKU-MOU-02",
            "INT-003",
            "Mouse Gamer RGB",
            "Mouse con luces",
            60m,
            5
        );

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(existingProduct);

        // 2. ACT
        Func<Task> act = async () => await _service.AddProduct(request);

        // 3. ASSERT
        var exception = await act.Should().ThrowAsync<DuplicatedEntityException>();
        exception.Which.ErrorCode.Should().Be(ErrorCodes.ProductoConMismoSku);

        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 100)]   // stock en cero
    [InlineData(-3, 100)]  // stock negativo
    [InlineData(10, -1)]   // precio negativo
    public async Task AddProduct_WithInvalidStockOrPrice_ThrowsInvalidDataException(int stock, decimal price)
    {
        // 1. ARRANGE
        var request = new ProductModel.RequestProduct(
            "SKU-MON-03",
            "INT-004",
            "Monitor 27",
            "Monitor IPS FHD",
            price,
            stock
        );

        // 2. ACT
        Func<Task> act = async () => await _service.AddProduct(request);

        // 3. ASSERT
        // En el proyecto los datos inválidos se informan con ValidationAppException
        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.ErrorCode.Should().Be(ErrorCodes.DatosInvalidos);
        exception.Which.Message.Should().NotBeNullOrWhiteSpace();

        // Se rechaza antes de consultar o guardar en el repositorio
        _repositoryMock.Verify(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }
}
