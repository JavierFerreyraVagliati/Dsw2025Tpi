using Dsw2025Tpi.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Application.Exceptions;

namespace Dsw2025Tpi.Api.Controllers
{

    [ApiController]

    [Route("api/products")]
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

    }
}
