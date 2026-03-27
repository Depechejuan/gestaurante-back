using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gestaurante.Models.DTO;
using Microsoft.IdentityModel.Tokens;

namespace Gestaurante.Models.Services
{
    public interface ICustomerJwtService
    {
        string GenerarToken(ClienteProfileDTO cliente);
        DateTime GetExpiracion();
    }

    public class CustomerJwtService : ICustomerJwtService
    {
        public string GenerarToken(ClienteProfileDTO cliente)
        {
            var key = Environment.GetEnvironmentVariable("CUSTOMER_JWT_KEY")
                ?? throw new InvalidOperationException("CUSTOMER_JWT_KEY no configurada.");
            var issuer = Environment.GetEnvironmentVariable("CUSTOMER_JWT_ISSUER")
                ?? throw new InvalidOperationException("CUSTOMER_JWT_ISSUER no configurado.");
            var audience = Environment.GetEnvironmentVariable("CUSTOMER_JWT_AUDIENCE")
                ?? throw new InvalidOperationException("CUSTOMER_JWT_AUDIENCE no configurado.");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, cliente.IdUsuarioCliente.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, cliente.Email),
                new Claim(ClaimTypes.NameIdentifier, cliente.IdUsuarioCliente.ToString()),
                new Claim(ClaimTypes.Email, cliente.Email),
                new Claim("scope", "customer")
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: GetExpiracion(),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiracion()
        {
            var days = Environment.GetEnvironmentVariable("CUSTOMER_JWT_EXPIRE_DAYS");
            return DateTime.UtcNow.AddDays(int.TryParse(days, out var parsed) ? parsed : 30);
        }
    }
}
