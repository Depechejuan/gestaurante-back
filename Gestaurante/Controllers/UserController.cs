using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
//using Gestaurante.Models.Seed;
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
        //private readonly RegisterService _registerService;


        public UserController(LoginService loginService, IJwtService jwtService, RegisterService registerService)
        {
            _loginService = loginService;
            _jwtService = jwtService;
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
    }
}
