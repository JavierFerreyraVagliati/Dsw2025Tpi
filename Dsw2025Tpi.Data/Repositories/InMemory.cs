using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using System.Linq.Expressions;
using System.Text.Json;


namespace Dsw2025Tpi.Data;

public class InMemory : IRepository
{
    private List<Customer>? _Customers;

    public InMemory()
    {
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Source\\customers.json"));
        _Customers = JsonSerializer.Deserialize<List<Customer>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private List<T>? GetList<T>() where T : EntityBase
    {
        return typeof(T).Name switch
        {
            nameof(Customer) => _Customers as List<T>,
            _ => throw new NotSupportedException(),
        };
    }

    public async Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase
    {
        return await Task.FromResult(GetList<T>()?.FirstOrDefault(e => e.Id == id));
    }

    public async Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase
    {
        return await Task.FromResult(GetList<T>());
    }

    public async Task<T> Add<T>(T entity) where T : EntityBase
    {
        GetList<T>()?.Add(entity);
        return await Task.FromResult(entity);
    }

    public Task<T> Update<T>(T entity) where T : EntityBase
    {
        throw new NotImplementedException();
    }

    public Task<T> Delete<T>(T entity) where T : EntityBase
    {
        throw new NotImplementedException();
    }

    public async Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        var Customer = GetList<T>()?.FirstOrDefault(predicate.Compile());
        return await Task.FromResult(Customer);
    }

    public async Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        var Customers = GetList<T>()?.Where(predicate.Compile());
        return await Task.FromResult(Customers);
    }
}