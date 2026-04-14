using Gestaurante.Configuration;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Gestaurante.Models.Services
{
    public interface IJwtService
    {
        string GenerarToken(EmpleadoLoginDTO empleado);
        DateTime GetExpiracion();
    }

    public class JwtService : IJwtService
    {
        private readonly EmployeeJwtOptions _options;

        public JwtService(IOptions<EmployeeJwtOptions> options)
        {
            _options = options.Value;
        }

        public string GenerarToken(EmpleadoLoginDTO empleado)
        {
            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, empleado.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, empleado.Email),
                new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                new Claim(ClaimTypes.Email, empleado.Email),
                new Claim(ClaimTypes.Role, empleado.Tipo.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: GetExpiracion(),
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiracion()
        {
            return DateTime.UtcNow.AddDays(_options.ExpireDays);
        }
    }
}
