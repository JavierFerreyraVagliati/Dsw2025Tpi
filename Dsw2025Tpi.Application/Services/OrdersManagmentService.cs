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
            var customer = await _repository.GetById<Customer>(request.CustomerId);

            if (customer == null) {
                throw new EntityNotFoundException(
                    $"Cliente con ID {request.CustomerId} no encontrado",
                    ErrorCodes.UsuarioNoEncontrado
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
                customer.Name,
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
                o.Customer.Name,
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
        public async Task<OrderModel.ResponsePagination> GetOrdersPagination(OrderModel.FilterOrders request)
        {
            string search = request.Search?.ToLower() ?? "";

            // Mapear string → enum OrderStatus
            OrderStatus? statusFilter = request.Status?.ToLower() switch
            {
                "pending" => OrderStatus.PENDING,
                "inprocess" => OrderStatus.PROCESSING,
                "cancelled" => OrderStatus.CANCELED,
                "delivered" => OrderStatus.DELIVERED,
                "shipped" => OrderStatus.SHIPPED,
                _ => null  // "all" → null
            };

            var filteredOrders = await _repository.GetFiltered<Order>(
                o =>
                    // ✔ filtro de estado (opcional)
                    (statusFilter == null || o.OrderStatus == statusFilter) &&

                    // ✔ filtro de búsqueda
                    (string.IsNullOrEmpty(search)
                        || (o.ShippingAddress != null && o.ShippingAddress.ToLower().Contains(search))
                        || (o.BillingAddress != null && o.BillingAddress.ToLower().Contains(search))
                        || (o.Notes != null && o.Notes.ToLower().Contains(search))),

                // Includes para EF
                "Customer",
                "OrderItem.Product"
            );

            if (filteredOrders == null || !filteredOrders.Any())
            {
                return new OrderModel.ResponsePagination(new List<OrderModel.Response>(), 0);
            }


            int pageNumber = request.PageNumber ?? 1;
            int pageSize = request.PageSize ?? filteredOrders.Count();

            var paginated = filteredOrders
                .OrderBy(o => o.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderModel.Response(
                    o.Id,
                    o.CustomerId,
                    o.Customer.Name,
                    o.ShippingAddress ?? "",
                    o.BillingAddress ?? "",
                    o.OrderStatus,
                    o.OrderItem.Select(i => new OrderItemModel.Request(
                        i.Quantity,
                        i.ProductId ?? Guid.Empty,
                        i.Product.Name,
                        i.Product.Description,
                        i.UnitPrice
                    )).ToList()
                ))
                .ToList();

            return new OrderModel.ResponsePagination(paginated, filteredOrders.Count());
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
                order.Customer.Name,
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
                order.Customer.Name,
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

