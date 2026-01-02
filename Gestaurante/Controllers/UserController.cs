using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly IJwtService _jwtService;

        public AuthController(LoginService loginService, IJwtService jwtService)
        {
            _loginService = loginService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            try
            {
                var empleado = await Task.Run(() => _loginService.Login(dto));

                if (empleado == null)
                    return Unauthorized(new
                    {
                        mensaje = "Credenciales inválidas",
                        codigo = "INVALID_CREDENTIALS"
                    });


                var token = _jwtService.GenerarToken(empleado);
                var expiracion = _jwtService.GetExpiracion();

                // Crear respuesta
                var response = new TokenDTO
                {
                    Id = empleado.Id,
                    Token = token,
                    ExpiraEn = expiracion
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor",
                    detalle = ex.Message
                });
            }
        }
    }
}
}
