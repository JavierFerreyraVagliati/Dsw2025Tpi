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
        private Order() { }

        public Order(string shippingAddress, string billingAddress, Guid customerId, List<OrderItem> orderItems)
        {
            ShippingAddress = shippingAddress;
            BillingAddress = billingAddress;
            CustomerId = customerId;
            Date = DateTime.Now;
            OrderItem = orderItems;
            TotalAmount = orderItems.Sum(item => item.Quantity * item.UnitPrice); // Supone esas propiedades
        }

        public DateTime Date { get; private set; }
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; } // corregido
        public string? Notes { get; set; }
        public decimal TotalAmount { get; private set; }

        public ICollection<OrderItem> OrderItem { get; set; } = new List<OrderItem>();
        public Customer? Customer { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public Guid? CustomerId { get; set; }
    }

}
