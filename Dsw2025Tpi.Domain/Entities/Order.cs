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
        public DateTime Date { get; } = DateTime.Now;
        public string? ShippingAddress { get; set; }
        public string? BilingAddress { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderItem> OrderItems { get; }
        public Customer? Customer { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public Guid? CustomerId { get; set; }

    }
}
