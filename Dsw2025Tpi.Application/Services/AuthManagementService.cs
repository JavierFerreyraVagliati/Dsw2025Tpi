using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Dsw2025Tpi.Application.Services
{
    public class AuthManagementService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IRepository _repository;

        public AuthManagementService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            JwtTokenService jwtTokenService,
            IRepository repository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _repository = repository;
        }

        public async Task<LoginModel.Response> LoginAsync(LoginModel.Request request)
        {
            // 1. Buscar usuario por username
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
                throw new ForbiddenException(
                    "Usuario o contraseña incorrectos",
                    ErrorCodes.DatosInvalidos
                );

            // 2. Verificar contraseña
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                throw new ForbiddenException(
                    "Usuario o contraseña incorrectos",
                    ErrorCodes.DatosInvalidos
                );

            // 3. Obtener el rol del usuario desde la BD
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null || !roles.Any())
                throw new ForbiddenException(
                    "El usuario no tiene roles asignados",
                    ErrorCodes.DatosInvalidos
                );

            var userRole = roles.First();

            // 4. Generar token JWT con el rol
            var token = _jwtTokenService.GenerateToken(user.UserName!, userRole);

            // 5. Preparar datos adicionales si es Customer
            object? profileData = null;

            if (userRole.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await _repository.First<Customer>(c => c.UserId == user.Id);

                if (customer != null)
                {
                    profileData = new
                    {
                        customerId = customer.Id,
                        customerName = customer.Name,
                        phoneNumber = customer.PhoneNumber
                    };
                }
            }

            // 6. Retornar DTO de respuesta
            return new LoginModel.Response
            (
                token,
                user.UserName!,
                user.Email!,
                userRole,
                profileData
            );
        }
    }
}
