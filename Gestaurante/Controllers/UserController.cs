using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
//using Gestaurante.Models.Seed;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly IJwtService _jwtService;
        private readonly StaffService _staffService;
        //private readonly RegisterService _registerService;


        public UserController(LoginService loginService, IJwtService jwtService, RegisterService registerService, StaffService staffService)
        {
            _loginService = loginService;
            _jwtService = jwtService;
            _staffService = staffService;
            //_registerService = registerService;

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            try
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
            catch
            {
                return ResponseHelper.ServerError("No se ha podido completar el inicio de sesión.");
            }
        }

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
