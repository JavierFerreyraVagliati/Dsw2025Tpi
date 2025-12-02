using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                throw new ValidationAppException("El nombre y el email son obligatorios.",ErrorCodes.DatosInvalidos);
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
        public async Task<Customer> CreateCustomerForUserAsync(IdentityUser user, string fullName, string email)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var customer = new Customer
            {
                UserId = user.Id,
                Name = fullName,
                Email = email
            };

           await  _repository.Add(customer);

            return customer;
        }

    }
}
