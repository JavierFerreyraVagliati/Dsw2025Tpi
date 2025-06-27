using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
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
            if (request.CustomerId == Guid.Empty ||
    string.IsNullOrWhiteSpace(request.ShippingAddress) ||
    string.IsNullOrWhiteSpace(request.BillingAddress))
            {
                throw new ArgumentException("Valores para la orden no válidos");
            }

            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                
                var product = await _repository.GetById<Product>(item.ProductId);
                if (product == null)
                    throw new ArgumentException($"Producto con ID {item.ProductId} no encontrado");

                if (product.StockQuantity >= item.Quantity)
                {
                    product.StockQuantity -= item.Quantity;
                }
                else
                {
                    throw new InsufficientStockException("Stock Insuficiente");
                }


                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.CurrentUnitPrice,
                };

                orderItems.Add(orderItem);
            }
                var order = new Order(
                    shippingAddress: request.ShippingAddress,
                    billingAddress: request.BillingAddress,
                    customerId: request.CustomerId,
                    orderItems: orderItems
                );


                await _repository.Add(order);
                return new OrderModel.Response(order.Id,request.CustomerId, request.ShippingAddress, request.BillingAddress,order.OrderStatus, request.Items);
            }

        public async Task<IEnumerable<OrderModel.Response>?> GetOrders()
        {
            var orders = await _repository.GetAll<Order>("OrderItem");

            return orders?.Select(o => new OrderModel.Response(o.Id,
                o.CustomerId,
                o.ShippingAddress,
                o.BillingAddress,
                o.OrderStatus,
                o.OrderItem
                    .Where(oi => oi.ProductId.HasValue)
                    .Select(oi => new OrderItemModel.Request(oi.Quantity, oi.ProductId.Value))
                    .ToList()
            ));
        }

        public async Task<OrderModel.Response?> GetOrderById(Guid id) {
            var order = await _repository.GetById<Order>(id,"OrderItem");

            return new OrderModel.Response(order.Id,order.CustomerId,
                order.ShippingAddress,
                order.BillingAddress,
                order.OrderStatus,
                order.OrderItem
                    .Where(oi => oi.ProductId.HasValue)
                    .Select(oi => new OrderItemModel.Request(oi.Quantity, oi.ProductId.Value))
                    .ToList());
        }

        public async Task<OrderModel.Response> UpdateOrderStatusAsync(Guid id, string newStatus)
        {
            var order = await _repository.GetById<Order>(id, "OrderItem");
            if (order == null)
                throw new EntityNotFoundException("Orden no encontrada.");

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsedStatus))
                throw new ArgumentException("Estado inválido.");

            order.OrderStatus = parsedStatus;
            await _repository.Update(order);

            return new OrderModel.Response(
                order.Id,
                order.CustomerId,
                order.ShippingAddress,
                order.BillingAddress,
                order.OrderStatus, // Asumo que este campo es string en el DTO
                order.OrderItem
                    .Where(oi => oi.ProductId.HasValue) // ← Evitás nulls
                    .Select(oi =>
                        new OrderItemModel.Request(
                            oi.Quantity,
                            oi.ProductId!.Value // ← Convertís Guid? a Guid seguro
                        )
                    )
                    .ToList()
            );
        }





    }
}

