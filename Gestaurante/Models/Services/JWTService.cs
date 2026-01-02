using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(EmpleadoLoginDTO empleado)
        {
            var claveSecreta = _configuration["Jwt:Key"];
            var emisor = _configuration["Jwt:Issuer"];
            var audiencia = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(claveSecreta))
                throw new ArgumentNullException("Jwt:Key no configurado");

            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
            var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, empleado.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, empleado.Email)
        };

            var token = new JwtSecurityToken(
                issuer: emisor,
                audience: audiencia,
                claims: claims,
                expires: GetExpiracion(),
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiracion()
        {
            var horas = _configuration.GetValue<int>("Jwt:ExpireHours", 24);
            return DateTime.UtcNow.AddHours(horas);
        }
    }
}
