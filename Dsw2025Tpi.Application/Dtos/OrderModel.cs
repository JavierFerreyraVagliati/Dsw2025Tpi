using Dsw2025Tpi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dsw2025Tpi.Application.Dtos.ProductModel;

namespace Dsw2025Tpi.Application.Dtos
{
    public record OrderModel
    {
        public record Request(Guid CustomerId, string ShippingAddress, string BillingAddress, List <OrderItemModel.Request> OrderItems);

        public record Response(Guid?OrderId,Guid? CustomerId,string NameCustomer, string ShippingAddress, string BillingAddress,OrderStatus orderStatus, List<OrderItemModel.Request>? OrderItems);

        public record ResponsePagination(List<OrderModel.Response> OrderItems, int Total);
        public record FilterOrders(string? Status, string? Search, int? PageNumber, int? PageSize);


    }
}
