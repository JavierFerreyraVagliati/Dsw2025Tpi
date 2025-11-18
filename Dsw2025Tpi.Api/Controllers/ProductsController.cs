using Dsw2025Tpi.Api.Extensions;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsManagmentService _service;

        public ProductsController(ProductsManagmentService service)
        {
            _service = service;
        }
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAuthProducts([FromQuery] ProductModel.FilterProduct filter)
        {
            var products = await _service.GetProducts(filter, isAdmin: true);
            return this.ApiOk(products, "Productos obtenidos correctamente");
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductModel.FilterProduct filter)
        {
            var products = await _service.GetProducts(filter, isAdmin: false);
            return this.ApiOk(products, "Productos obtenidos correctamente");
        }

     
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _service.GetProductById(id);

            return this.ApiOk(product, "Producto obtenido correctamente");
        }

     
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel.RequestProduct request)
        {
            var product = await _service.AddProduct(request);

            return this.ApiCreated(product, "Producto creado correctamente");
        }

    
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PutProduct(Guid id, [FromBody] ProductModel.RequestProduct request)
        {
            var product = await _service.PutProduct(id, request);

            return this.ApiOk(product, "Producto actualizado correctamente");
        }

       
        [HttpPatch("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> InactivateProduct(Guid id)
        {
            await _service.InactivateProduct(id);

            return this.ApiNoContent();
        }
    }
}
