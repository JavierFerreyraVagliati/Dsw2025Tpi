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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomerManagmentService(
            IRepository repository,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _repository = repository;
            _userManager = userManager;
            _roleManager = roleManager;
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

        public async Task<IdentityResult> CreateCustomerUserAsync(RegisterModel model)
        {
            // Validaciones básicas
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                throw new ArgumentException("Email y Password son obligatorios");

            if (string.IsNullOrEmpty(model.Username))
                throw new ArgumentException("Username es obligatorio");

            // Verificar que el usuario no exista
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
                throw new ConflictException("El usuario ya existe", ErrorCodes.DatosInvalidos);

            // Verificar que el email no esté en uso
            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
                throw new ConflictException("El email ya está en uso", ErrorCodes.DatosInvalidos);

            // Verificar que el rol exista
            if (!await _roleManager.RoleExistsAsync(model.Role))
                throw new ValidationAppException($"El rol {model.Role} no existe", ErrorCodes.DatosInvalidos);

            // Crear usuario de Identity
            var identityUser = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(identityUser, model.Password);

            if (!result.Succeeded)
                return result;

            // Agregar rol al usuario
            await _userManager.AddToRoleAsync(identityUser, model.Role);

            // ✅ Solo crear Customer si el rol es "Customer"
            if (model.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                using var transaction = await _repository.BeginTransactionAsync();
                try
                {
                    var customer = new Customer
                    {
                        UserId = identityUser.Id,
                        Name = model.Username,
                        Email = model.Email,
                    };

                    await _repository.Add(customer);
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    // Si falla la creación del Customer, eliminar el usuario de Identity
                    await _userManager.DeleteAsync(identityUser);
                    throw;
                }
            }

            return result;
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

           await _repository.Add(customer);

            return customer;
        }

    }
}
