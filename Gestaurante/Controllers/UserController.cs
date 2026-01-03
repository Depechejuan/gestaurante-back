using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Gestaurante.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LoginService _loginService;
        private readonly IJwtService _jwtService;

        public UserController(LoginService loginService, IJwtService jwtService)
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

                var response = new TokenDTO(token, expiracion, empleado.Id);

                return ResponseHelper.SendResponse(response);
            }
            catch (Exception ex)
            {
                return ResponseHelper.SendError(ex, 500);
            }
        }
    }
}
