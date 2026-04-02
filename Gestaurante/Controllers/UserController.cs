using Gestaurante.Models.DTO;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gestaurante.Controllers
{
    /// <summary>
    /// Gestiona la autenticación y el perfil básico de empleados internos.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly IJwtService _jwtService;
        private readonly StaffService _staffService;

        /// <summary>
        /// Inicializa el controlador con los servicios de login, JWT y staff.
        /// </summary>
        /// <param name="loginService">Servicio de validación de credenciales internas.</param>
        /// <param name="jwtService">Servicio generador del JWT de empleados.</param>
        /// <param name="registerService">Dependencia histórica del controlador.</param>
        /// <param name="staffService">Servicio de resolución del perfil básico del empleado.</param>
        public UserController(LoginService loginService, IJwtService jwtService, RegisterService registerService, StaffService staffService)
        {
            _loginService = loginService;
            _jwtService = jwtService;
            _staffService = staffService;
        }

        /// <summary>
        /// Valida las credenciales de un empleado y devuelve un token de acceso.
        /// </summary>
        /// <param name="dto">Credenciales de acceso del empleado.</param>
        /// <returns>Respuesta HTTP con el token emitido o un 401 si las credenciales son inválidas.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var empleado = await _loginService.Login(dto);

            if (empleado == null)
                return ResponseHelper.NotAuthorized(new
                {
                    mensaje = "Credenciales inválidas",
                    codigo = "INVALID_CREDENTIALS"
                });

            var token = _jwtService.GenerarToken(empleado);
            var expiracion = _jwtService.GetExpiracion();

            var response = new TokenDTO(token, expiracion, empleado.Id, empleado.Tipo);

            return ResponseHelper.SendResponse(response, 201);
        }

        /// <summary>
        /// Recupera la información básica del empleado autenticado.
        /// </summary>
        /// <returns>Respuesta HTTP con el perfil básico del empleado autenticado.</returns>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(employeeId, out var id))
                return ResponseHelper.NotAuthorized(new
                {
                    mensaje = "No se ha podido validar la sesión.",
                    codigo = "INVALID_SESSION"
                });

            var empleado = await _staffService.GetBasicStaff(id);
            if (empleado == null)
                return ResponseHelper.NotAuthorized(new
                {
                    mensaje = "La sesión ya no es válida.",
                    codigo = "SESSION_NOT_FOUND"
                });

            return ResponseHelper.SendResponse(empleado);
        }
    }
}
