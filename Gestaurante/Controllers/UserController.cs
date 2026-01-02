using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Gestaurante.Models.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gestaurante.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO dto)
        {
            var empleado = _authService.Login(dto);

            if (empleado == null)
                return Unauthorized("Credenciales inválidas");

            var hasher = new PasswordHasher<Empleado>();
            var result = hasher.VerifyHashedPassword(
                empleado,
                empleado.PasswordHash,
                dto.Password
            );

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Credenciales inválidas");

            var token = _jwtService.GenerateToken(empleado);

            return Ok(new
            {
                token
            });
        }

    }
}
