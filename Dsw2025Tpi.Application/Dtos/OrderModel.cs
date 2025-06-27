using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Domain.Enums;

namespace Dsw2025Tpi.Application.Dtos
{
    public record OrderModel
    {
        public record Request(Guid CustomerId, string ShippingAddress, string BillingAddress, List <OrderItemModel.Request> Items);

        public record Response(Guid?OrderId,Guid? CustomerId, string ShippingAddress, string BillingAddress,OrderStatus orderStatus, List<OrderItemModel.Request>? Items);
    }
}
