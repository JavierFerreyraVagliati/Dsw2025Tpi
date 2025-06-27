using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly IRepository _repository;
        private readonly CustomerManagmentService _customerManagmentService;

        public CustomersController(CustomerManagmentService customerManagmentService)
        {
            _customerManagmentService = customerManagmentService;
        }

        [HttpPost]
        public async Task<IActionResult> CrearCliente([FromBody] CustomerModel.Request request)
        {
            try
            {
                var id = await _customerManagmentService.AddCustomer(request);
                return Ok(new { Id = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return Problem("Se produjo un error al crear el cliente.");
            }
        }
    }

  

}
