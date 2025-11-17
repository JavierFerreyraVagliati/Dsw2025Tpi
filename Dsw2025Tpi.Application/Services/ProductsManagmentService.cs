using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Dsw2025Tpi.Domain;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Dsw2025Tpi.Application.Services;

public class ProductsManagmentService
{
    private readonly IRepository _repository;

    public ProductsManagmentService(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductModel.ResponseProduct?> GetProductById(Guid id)
    {
        var product = await _repository.GetById<Product>(id);
        return product != null ?
            new ProductModel.ResponseProduct(product.Id, product.Sku, product.InternalCode, product.Name, product.Description, 
            product.CurrentUnitPrice, product.StockQuantity,product.IsActive) :
            null;
    }

    public async Task<ProductModel.ResponsePagination?> GetProducts(ProductModel.FilterProduct request,bool isAdmin)
    {
        var isActive = request.Status == "enabled"
            ? (bool?)true
            : request.Status == "disabled"
            ? (bool?)false
            : null;

        if (!isAdmin)
            isActive = true;

        var activeProducts = await _repository.GetFiltered<Product>(
            p => (isActive == null || p.IsActive == isActive) &&
                 (string.IsNullOrEmpty(request.Search) || p.Name.Contains(request.Search))
        );

        if (activeProducts is null || !activeProducts.Any())
            throw new NotContentException("No se encontraron productos con los filtros indicados", ErrorCodes.SinProductosEncontrados);

        var products = activeProducts
            .Select(p => new ProductModel.ResponseProduct(
                p.Id,
                p.Sku,
                p.InternalCode,
                p.Name,
                p.Description,
                p.CurrentUnitPrice,
                p.StockQuantity,
                p.IsActive))
            .OrderBy(p => p.Sku)
            .Skip(((request.PageNumber ?? 1) - 1) * (request.PageSize ?? 0))
            .Take(request.PageSize ?? activeProducts.Count());

        return new ProductModel.ResponsePagination(products.ToList(), activeProducts.Count());
    }

    public async Task<ProductModel.ResponseProduct> AddProduct(ProductModel.RequestProduct request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            request.CurrentUnitPrice < 0 || 
            request.StockQuantity < 0
            )
        {
            throw new ValidationAppException("Valores para el producto no válidos", ErrorCodes.DatosInvalidos);
        }

        var exist = await _repository.First<Product>(p => p.Sku == request.Sku);
        if (exist != null)
            throw new DuplicatedEntityException($"Ya existe un producto con el Sku {request.Sku}", ErrorCodes.ProductoConMismoSku);

        var product = new Product(request.Sku, request.InternalCode, request.Name, request.Description, request.CurrentUnitPrice, request.StockQuantity);
        await _repository.Add(product);
        return new ProductModel.ResponseProduct(product.Id, product.Sku, product.InternalCode, product.Name, product.Description,
            product.CurrentUnitPrice, product.StockQuantity,product.IsActive);
    }

    public async Task<ProductModel.ResponseProduct> PutProduct(Guid id, ProductModel.RequestProduct request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.CurrentUnitPrice < 0 ||
            request.StockQuantity < 0)
        {
            throw new ValidationAppException("Valores para el producto no válidos", ErrorCodes.DatosInvalidos);
        }

        var exist = await _repository.GetById<Product>(id)
            ?? throw new EntityNotFoundException($"No se encontró el producto con ID {id}", ErrorCodes.ProductoNoEncontrado);

        exist.Sku = request.Sku; 
        exist.InternalCode = request.InternalCode;
        exist.Name = request.Name;
        exist.Description = request.Description;
        exist.CurrentUnitPrice = request.CurrentUnitPrice;
        exist.StockQuantity = request.StockQuantity;

        await _repository.Update(exist);

        return new ProductModel.ResponseProduct(
            exist.Id,
            exist.Sku,
            exist.InternalCode,
            exist.Name,
            exist.Description,
            exist.CurrentUnitPrice,
            exist.StockQuantity,
            exist.IsActive
    );
    }
    public async Task InactivateProduct(Guid id)
    {
        var exist = await _repository.GetById<Product>(id)
            ?? throw new EntityNotFoundException($"No se encontró el producto con ID {id}", ErrorCodes.ProductoNoEncontrado);

        exist.IsActive = false;

        await _repository.Update(exist);
    }

}
