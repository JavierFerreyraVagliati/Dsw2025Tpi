using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Domain;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Dsw2025Tpi.Application.Services;

public class ProductsManagmentService
{
    private readonly IRepository _repository;

    public ProductsManagmentService(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductModel.Response?> GetProductById(Guid id)
    {
        var product = await _repository.GetById<Product>(id);
        return product != null ?
            new ProductModel.Response(product.Id, product.Sku, product.InternalCode, product.Name, product.Description, 
            product.CurrentUnitPrice, product.StockQuantity) :
            null;
    }

    public async Task<IEnumerable<ProductModel.Response>?> GetProducts()
    {
        return (await _repository
            .GetFiltered<Product>(p => p.IsActive))?
            .Select(p => new ProductModel.Response(p.Id, p.Sku, p.InternalCode, p.Name, p.Description, 
            p.CurrentUnitPrice, p.StockQuantity));
    }

    public async Task<ProductModel.Response> AddProduct(ProductModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            request.CurrentUnitPrice < 0 || 
            request.StockQuantity <0
            )
        {
            throw new ArgumentException("Valores para el producto no válidos");
        }

        var exist = await _repository.First<Product>(p => p.Sku == request.Sku);
        if (exist != null) throw new DuplicatedEntityException($"Ya existe un producto con el Sku {request.Sku}");
        var product = new Product(request.Sku, request.InternalCode, request.Name, request.Description, request.CurrentUnitPrice, request.StockQuantity);
        await _repository.Add(product);
        return new ProductModel.Response(product.Id, product.Sku, product.InternalCode, product.Name, product.Description,
            product.CurrentUnitPrice, product.StockQuantity);
    }

    public async Task<ProductModel.Response> PutProduct(ProductModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            request.CurrentUnitPrice < 0 ||
            request.StockQuantity < 0
            )
        {
            throw new ArgumentException("Valores para el producto no válidos");
        }
        var exist = await _repository.First<Product>(p => p.Sku == request.Sku) ?? throw new EntityNotFoundException($"No se encontró la entidad");
        exist.InternalCode = request.InternalCode;
        exist.Name = request.Name;
        exist.Description = request.Description;
        exist.CurrentUnitPrice = request.CurrentUnitPrice;
        exist.StockQuantity = request.StockQuantity;
        await _repository.Update(exist);
        return new ProductModel.Response(exist.Id, exist.Sku, exist.InternalCode, exist.Name, exist.Description,
            exist.CurrentUnitPrice, exist.StockQuantity); ;
    }
    public async Task InactivateProduct(Guid id)
    {
        var exist = await _repository.GetById<Product>(id)
            ?? throw new EntityNotFoundException($"No se encontró el producto con ID {id}");
        if (exist.IsActive == true) { 
        }
        exist.IsActive = false;

        await _repository.Update(exist);
    }

}
