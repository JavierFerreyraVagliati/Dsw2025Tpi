using Dsw2025Tpi.Api.Extensions;
using Dsw2025Tpi.Application.Common.Errors;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly JwtTokenService _jwtTokenService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly CustomerManagmentService _customerService;
        private readonly AuthManagementService _authManagementService;

        public AuthenticateController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            JwtTokenService jwtTokenService,
            RoleManager<IdentityRole> roleManager,
            CustomerManagmentService customerService,
            AuthManagementService authManagementService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _roleManager = roleManager;
            _customerService = customerService;
            _authManagementService = authManagementService;
        }

        // ============================
        // LOGIN
        // ============================
        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginModel request)
        //{
        //    var user = await _userManager.FindByNameAsync(request.Username);

        //    if (user == null)
        //        throw new ForbiddenException(
        //            "Usuario o contraseña incorrectos",
        //            ErrorCodes.DatosInvalidos
        //        );

        //    var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        //    if (!result.Succeeded)
        //        throw new ForbiddenException(
        //            "Usuario o contraseña incorrectos",
        //            ErrorCodes.DatosInvalidos
        //        );

        //    // En una app real, deberías obtener el rol desde la BD
        //    var token = _jwtTokenService.GenerateToken(user.UserName!, "admin");

        //    return this.ApiOk(new { token }, "Login exitoso");
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel.Request request)
        {
            var response = await _authManagementService.LoginAsync(request);
            return this.ApiOk(response, "Login exitoso");
        }

        // ============================
        // REGISTER
        // ============================
        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] RegisterModel model)
        //{
        //    var existing = await _userManager.FindByNameAsync(model.Username);
        //    if (existing != null)
        //        throw new ConflictException(
        //            "El usuario ya existe",
        //            ErrorCodes.DatosInvalidos
        //        );

        //    var user = new IdentityUser
        //    {
        //        UserName = model.Username,
        //        Email = model.Email,

        //    };

        //    var result = await _userManager.CreateAsync(user, model.Password);
        //    if (!await _roleManager.RoleExistsAsync(model.Role))
        //    {
        //        throw new ValidationAppException($"El rol {model.Role} no existe", ErrorCodes.DatosInvalidos);
        //    }
        //    await _userManager.AddToRoleAsync(user, model.Role);

        //    if (!result.Succeeded)
        //    {
        //        // normalizamos errores de Identity en un BAD REQUEST
        //        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));

        //        throw new ValidationAppException(
        //            errorMessage,
        //            ErrorCodes.DatosInvalidos
        //        );
        //    }

        //    return this.ApiCreated(new { username = model.Username }, "Usuario registrado correctamente");
        //}
        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] RegisterModel model)
        //{
        //    // ✅ 1. Validar que el usuario NO exista
        //    var existing = await _userManager.FindByNameAsync(model.Username);
        //    if (existing != null)
        //        throw new ConflictException("El usuario ya existe", ErrorCodes.DatosInvalidos);

        //    // ✅ 2. Validar que el rol exista ANTES de crear el usuario
        //    if (!await _roleManager.RoleExistsAsync(model.Role))
        //        throw new ValidationAppException($"El rol {model.Role} no existe", ErrorCodes.DatosInvalidos);

        //    // ✅ 3. Crear el usuario
        //    var user = new IdentityUser
        //    {
        //        UserName = model.Username,
        //        Email = model.Email,
        //    };

        //    var result = await _userManager.CreateAsync(user, model.Password);

        //    // ✅ 4. Validar resultado ANTES de continuar
        //    if (!result.Succeeded)
        //    {
        //        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        //        throw new ValidationAppException(errorMessage, ErrorCodes.DatosInvalidos);
        //    }

        //    // ✅ 5. Ahora sí agregar el rol (usuario ya fue creado exitosamente)
        //    await _userManager.AddToRoleAsync(user, model.Role);

        //    // ✅ 6. Crear Customer si aplica
        //    if (model.Role.Equals("customer", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var customer = await _customerService.CreateCustomerForUserAsync(user, model.Username, model.Email);
        //        return this.ApiCreated(
        //            new { username = model.Username, customerId = customer.Id },
        //            "Usuario y cliente registrados correctamente"
        //        );
        //    }

        //    return this.ApiCreated(new { username = model.Username }, "Usuario registrado correctamente");
        //}

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var result = await _customerService.CreateCustomerUserAsync(model);

                if (!result.Succeeded)
                {
                    var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new ValidationAppException(errorMessage, ErrorCodes.DatosInvalidos);
                }

                return this.ApiCreated(
                    new { username = model.Username },
                    "Usuario registrado correctamente"
                );
            }
            catch (ConflictException ex)
            {
                throw; // Deja que tu middleware global lo maneje
            }
            catch (ValidationAppException ex)
            {
                throw; // Deja que tu middleware global lo maneje
            }
            catch (Exception ex)
            {
                throw new ValidationAppException(
                    "Error al registrar usuario: " + ex.Message,
                    ErrorCodes.DatosInvalidos
                );
            }
        }
        }
}
