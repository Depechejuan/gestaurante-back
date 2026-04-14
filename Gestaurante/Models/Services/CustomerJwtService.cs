using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gestaurante.Configuration;
using Gestaurante.Models.DTO;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public interface ICustomerJwtService
    {
        string GenerarToken(ClienteProfileDTO cliente);
        DateTime GetExpiracion();
    }

    public class CustomerJwtService : ICustomerJwtService
    {
        private readonly CustomerJwtOptions _options;

        public CustomerJwtService(IOptions<CustomerJwtOptions> options)
        {
            _options = options.Value;
        }

        public string GenerarToken(ClienteProfileDTO cliente)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
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
                _options.Issuer,
                _options.Audience,
                claims,
                expires: GetExpiracion(),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiracion()
        {
            return DateTime.UtcNow.AddDays(_options.ExpireDays);
        }
    }
}
