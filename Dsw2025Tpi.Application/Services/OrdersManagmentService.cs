using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Entities;
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
                return new OrderModel.Response(request.CustomerId, request.ShippingAddress, request.BillingAddress, request.Items);
            }

        }
    }

