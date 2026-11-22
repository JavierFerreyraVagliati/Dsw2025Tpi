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

public class OrdersManagmentServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly OrdersManagmentService _service;

    public OrdersManagmentServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new OrdersManagmentService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        // 1. ARRANGE
        var customer = new Customer
        {
            Name = "Javier Ferreyra"
        };

        var product1 = new Product
        {
            Name = "Teclado Mecánico",
            CurrentUnitPrice = 100m,
            StockQuantity = 10
        };

        var product2 = new Product
        {
            Name = "Mouse Gamer",
            CurrentUnitPrice = 50m,
            StockQuantity = 8
        };

        var itemsRequest = new List<OrderItemModel.Request>
        {
            new(2, product1.Id, product1.Name, "Teclado switch rojo", product1.CurrentUnitPrice),
            new(3, product2.Id, product2.Name, "Mouse inalámbrico", product2.CurrentUnitPrice)
        };

        var request = new OrderModel.Request(
            customer.Id,
            "Av. Aconquija 1200",
            "Av. Aconquija 1200",
            itemsRequest
        );

        Order? capturedOrder = null;

        _repositoryMock
            .Setup(r => r.GetById<Customer>(customer.Id))
            .ReturnsAsync(customer);

        _repositoryMock
            .Setup(r => r.GetById<Product>(product1.Id))
            .ReturnsAsync(product1);

        _repositoryMock
            .Setup(r => r.GetById<Product>(product2.Id))
            .ReturnsAsync(product2);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .ReturnsAsync((Order o) => o);

        // 2. ACT
        var response = await _service.AddOrder(request);

        // 3. ASSERT
        response.Should().NotBeNull();
        response.CustomerId.Should().Be(customer.Id);
        response.NameCustomer.Should().Be("Javier Ferreyra");

        product1.StockQuantity.Should().Be(8);
        product2.StockQuantity.Should().Be(5);

        capturedOrder.Should().NotBeNull();
        var expectedTotal = (2 * 100m) + (3 * 50m);
        var calculatedTotal = capturedOrder!.OrderItem.Sum(i => i.Quantity * i.UnitPrice);
        calculatedTotal.Should().Be(expectedTotal);

        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // 1. ARRANGE
        var customer = new Customer
        {
            Name = "Javier Ferreyra"
        };

        var product = new Product
        {
            Name = "Monitor 27",
            CurrentUnitPrice = 300m,
            StockQuantity = 2
        };

        var request = new OrderModel.Request(
            customer.Id,
            "San Martín 450",
            "San Martín 450",
            new List<OrderItemModel.Request>
            {
                new(5, product.Id, product.Name, "Monitor IPS FHD", product.CurrentUnitPrice)
            }
        );

        _repositoryMock
            .Setup(r => r.GetById<Customer>(customer.Id))
            .ReturnsAsync(customer);

        _repositoryMock
            .Setup(r => r.GetById<Product>(product.Id))
            .ReturnsAsync(product);

        // 2. ACT
        Func<Task> act = async () => await _service.AddOrder(request);

        // 3. ASSERT
        var exception = await act.Should().ThrowAsync<InsufficientStockException>();
        exception.Which.ErrorCode.Should().Be(ErrorCodes.StockInsuficiente);

        product.StockQuantity.Should().Be(2);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        // 1. ARRANGE
        var nonExistentCustomerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new OrderModel.Request(
            nonExistentCustomerId,
            "Belgrano 800",
            "Belgrano 800",
            new List<OrderItemModel.Request>
            {
                new(1, productId, "Auriculares", "Auriculares gamer", 80m)
            }
        );

        _repositoryMock
            .Setup(r => r.GetById<Customer>(nonExistentCustomerId))
            .ReturnsAsync((Customer)null!);

        // 2. ACT
        Func<Task> act = async () => await _service.AddOrder(request);

        // 3. ASSERT
        var exception = await act.Should().ThrowAsync<EntityNotFoundException>();
        exception.Which.ErrorCode.Should().Be(ErrorCodes.UsuarioNoEncontrado);

        _repositoryMock.Verify(r => r.GetById<Product>(It.IsAny<Guid>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }
}