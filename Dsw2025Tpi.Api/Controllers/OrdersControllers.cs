using Dsw2025Tpi.Api.Extensions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersControllers : ControllerBase
    {
        private readonly OrdersManagmentService _service;

        public OrdersControllers(OrdersManagmentService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "customer,admin,seller")]
        public async Task<IActionResult> AddOrder([FromBody] OrderModel.Request request)
        {
            var order = await _service.AddOrder(request);
            return this.ApiCreated(order, "Order created successfully");
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetOrders([FromQuery] OrderModel.FilterOrders request)
        {
            var orders = await _service.GetOrdersPagination(request);
            return this.ApiOk(orders, "Ordenes obtenidas correctamente");
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var order = await _service.GetOrderById(id);
            return this.ApiOk(order, "Orden obtenida correctamente"); 
        }

        // PUT: api/orders/{id}/status
        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusModel.Request request)
        {
            await _service.UpdateOrderStatusAsync(id, request.NewStatus);
            return this.ApiOk("Orden actualizada correctamente");
        }
    }
}
