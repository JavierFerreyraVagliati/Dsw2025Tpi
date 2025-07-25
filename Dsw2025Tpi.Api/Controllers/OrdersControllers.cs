﻿using Dsw2025Tpi.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Application.Exceptions;

namespace Dsw2025Tpi.Api.Controllers
{

    [ApiController]

    [Route("api/orders")]
    public class OrdersControllers : ControllerBase
    {
        private readonly OrdersManagmentService _service;
        public OrdersControllers(OrdersManagmentService service) { 
           _service = service;
        }  
        [HttpPost]
        public async Task<IActionResult> AddOrder([FromBody] OrderModel.Request request)
        {
            try
            {
                var product = await _service.AddOrder(request);
                return Ok(product);
            }
            catch (ArgumentException ae)
            {
                return BadRequest(ae.Message);
            }
            catch (Exception)
            {
                return Problem("Se produjo un error al guardar el producto");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders() {
            var orders = await _service.GetOrders();
            if (orders == null || !orders.Any()) return NoContent();
            return Ok(orders);

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var product = await _service.GetOrderById(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusModel.Request request)
        {
            try
            {
                var updatedOrder = await _service.UpdateOrderStatusAsync(id, request.NewStatus);

                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return Problem("Se produjo un error al actualizar el estado de la orden.");
            }
        }




    }
}
