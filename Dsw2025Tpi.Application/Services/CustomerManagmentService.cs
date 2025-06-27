using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Application.Services
{
    public class CustomerManagmentService
    {
        private readonly IRepository _repository;

        public CustomerManagmentService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> AddCustomer(CustomerModel.Request request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("El nombre y el email son obligatorios.");
            }
            var nuevoCliente = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber
            };

            await _repository.Add(nuevoCliente);
            return nuevoCliente.Id;
        }
    }
}
