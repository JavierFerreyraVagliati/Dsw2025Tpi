using Microsoft.AspNetCore.Mvc;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Application.Exceptions;

namespace Dsw2025Tpi.Api.Controllers;

[ApiController]

[Route("api/products")]

public class ProductsController : ControllerBase
{
    private readonly ProductsManagmentService _service;

    public ProductsController(ProductsManagmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _service.GetProducts();
        if (products == null || !products.Any()) return NoContent();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductBySku(Guid id)
    {
        var product = await _service.GetProductById(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost()]
    public async Task<IActionResult> AddProduct([FromBody] ProductModel.Request request)
    {
        try
        {
            var product = await _service.AddProduct(request);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct([FromBody] ProductModel.Request request)
    {
        try
        {
            var product = await _service.PutProduct(request);
            return Ok(product);
        }
        catch (ArgumentException ae)
        {
            return BadRequest(ae.Message);
        }
        catch (EntityNotFoundException ae)
        {
            return NotFound(ae.Message);
        }
        
        catch (Exception)
        {
            return Problem("Se produjo un error al guardar el producto");
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> InactivateProduct(Guid id) {

        try
        {
            await _service.InactivateProduct(id);
            return Ok();
        }
        catch(EntityNotFoundException en) {
            return BadRequest(en.Message);
        }
    }

}
