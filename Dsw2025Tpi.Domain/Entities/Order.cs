using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Domain.Enums;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Order : EntityBase
    {
        private readonly List<OrderItem> _orderItems = new();
        private Order() { }
        public Order(string shippingAddress, string billingAddress, Guid customerId, List<OrderItem> items)
        {
            ShippingAddress = shippingAddress;
            BilingAddress = billingAddress;
            CustomerId = customerId;
            Date = DateTime.Now;
            foreach (var item in items)
            {
                if (item.Product == null)
                    throw new InvalidOperationException("El producto no puede ser nulo.");

                if (item.Quantity > item.Product.StockQuantity)
                    throw new InvalidOperationException($"Stock insuficiente para el producto: {item.Product.Name}");

                item.Product.StockQuantity -= item.Quantity; // Descontar stock
                _orderItems.Add(item);
            }
            TotalAmount = _orderItems.Sum(i => i.SubTotal);
        }

        public DateTime Date { get; private set; }
        public string? ShippingAddress { get; set; }
        public string? BilingAddress { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAmount { get; private set; }
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
        public Customer? Customer { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public Guid? CustomerId { get; set; }
    }

}
