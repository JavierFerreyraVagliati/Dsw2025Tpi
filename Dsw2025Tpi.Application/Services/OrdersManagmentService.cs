using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Enums;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Application.Services
{
    public class OrdersManagmentService
    {
        private readonly IRepository _repository;

        public OrdersManagmentService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<OrderModel.Response> AddOrder(OrderModel.Request request)
        {
            // VALIDACIÓN BÁSICA
            if (request.CustomerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.ShippingAddress) ||
                string.IsNullOrWhiteSpace(request.BillingAddress))
            {
                throw new ValidationAppException(
                    "Valores para la orden no válidos",
                    ErrorCodes.DatosInvalidos
                );
            }

            var orderItems = new List<OrderItem>();

            foreach (var item in request.OrderItems)
            {
                var product = await _repository.GetById<Product>(item.ProductId);

                if (product == null)
                {
                    throw new EntityNotFoundException(
                        $"Producto con ID {item.ProductId} no encontrado",
                        ErrorCodes.ProductoNoEncontrado
                    );
                }

                // STOCK INSUFICIENTE
                if (product.StockQuantity < item.Quantity)
                {
                    throw new InsufficientStockException(
                        $"Stock insuficiente para el producto '{product.Name}'. Solicitado: {item.Quantity}, Disponible: {product.StockQuantity}",
                        ErrorCodes.StockInsuficiente
                    );
                }

                // Descontar stock
                product.StockQuantity -= item.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.CurrentUnitPrice,
                };

                orderItems.Add(orderItem);
            }

            // CREACIÓN DE ORDEN
            var order = new Order(
                shippingAddress: request.ShippingAddress,
                billingAddress: request.BillingAddress,
                customerId: request.CustomerId,
                orderItems: orderItems
            );

            await _repository.Add(order);

            return new OrderModel.Response(
                order.Id,
                request.CustomerId,
                request.ShippingAddress,
                request.BillingAddress,
                order.OrderStatus,
                request.OrderItems
            );
        }

        public async Task<IEnumerable<OrderModel.Response>?> GetOrders()
        {
            var orders = await _repository.GetAll<Order>("OrderItem.Product");

            return orders?.Select(o => new OrderModel.Response(
                o.Id,
                o.CustomerId,
                o.ShippingAddress,
                o.BillingAddress,
                o.OrderStatus,
                o.OrderItem
                    .Where(oi => oi.ProductId.HasValue)
                    .Select(oi => new OrderItemModel.Request(
                        oi.Quantity,
                        oi.ProductId.Value,
                        oi.Product?.Name,
                        oi.Product?.Description,
                        oi.Product?.CurrentUnitPrice
                    )).ToList()
            ));
        }

        public async Task<OrderModel.Response?> GetOrderById(Guid id)
        {
            var order = await _repository.GetById<Order>(id, "OrderItem.Product");

            if (order == null)
            {
                throw new EntityNotFoundException(
                    $"No se encontró la orden con ID {id}",
                    ErrorCodes.PedidoNoEncontrado
                );
            }

            return new OrderModel.Response(
                order.Id,
                order.CustomerId,
                order.ShippingAddress,
                order.BillingAddress,
                order.OrderStatus,
                order.OrderItem
                    .Where(oi => oi.ProductId.HasValue)
                    .Select(oi => new OrderItemModel.Request(
                        oi.Quantity,
                        oi.ProductId.Value,
                        oi.Product?.Name,
                        oi.Product?.Description,
                        oi.Product.CurrentUnitPrice
                    )).ToList()
            );
        }

        public async Task<OrderModel.Response> UpdateOrderStatusAsync(Guid id, string newStatus)
        {
            var order = await _repository.GetById<Order>(id, "OrderItem");

            if (order == null)
                throw new EntityNotFoundException("Orden no encontrada.", ErrorCodes.PedidoNoEncontrado);

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsedStatus))
                throw new ValidationAppException("Estado de la orden inválido.", ErrorCodes.DatosInvalidos);

            order.OrderStatus = parsedStatus;
            await _repository.Update(order);

            return new OrderModel.Response(
                order.Id,
                order.CustomerId,
                order.ShippingAddress,
                order.BillingAddress,
                order.OrderStatus,
                order.OrderItem
                    .Where(oi => oi.ProductId.HasValue)
                    .Select(oi => new OrderItemModel.Request(
                        oi.Quantity,
                        oi.ProductId.Value,
                        oi.Product.Name,
                        oi.Product.Description,
                        oi.Product.CurrentUnitPrice
                    ))
                    .ToList()
            );
        }
    }
}

