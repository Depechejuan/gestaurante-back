using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Gestaurante.Configuration;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    /// <summary>
    /// Gestiona tokens de un solo uso para acciones públicas de cuenta.
    /// </summary>
    public class AccountActionTokenService
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly FrontendOptions _frontendOptions;

        /// <summary>
        /// Inicializa el servicio de tokens de cuenta.
        /// </summary>
        public AccountActionTokenService(
            AppDbContext db,
            IEmailService emailService,
            IOptions<FrontendOptions> frontendOptions)
        {
            _db = db;
            _emailService = emailService;
            _frontendOptions = frontendOptions.Value;
        }

        /// <summary>
        /// Solicita enlaces de recuperación para todas las cuentas activas asociadas al email.
        /// </summary>
        /// <remarks>
        /// La respuesta HTTP del controlador debe ser genérica para no revelar si la cuenta existe.
        /// </remarks>
        public async Task RequestPasswordResetAsync(ForgotPasswordDTO dto, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(dto.Email);
            var loweredEmail = email.ToLower();

            var employees = await _db.Empleados
                .Where(empleado => empleado.Activo && empleado.Email.ToLower() == loweredEmail)
                .ToListAsync(cancellationToken);

            foreach (var employee in employees)
            {
                var link = await CreateLinkAsync(
                    AccountActionTokenUserType.Employee,
                    AccountActionTokenPurpose.PasswordReset,
                    employee.Id,
                    employee.Email,
                    "/restablecer-password",
                    cancellationToken);

                await SendPasswordResetEmailAsync(employee.Email, link, cancellationToken);
            }

            var customers = await _db.UsuariosCliente
                .Where(cliente => cliente.Activo && cliente.Email.ToLower() == loweredEmail)
                .ToListAsync(cancellationToken);

            foreach (var customer in customers)
            {
                var link = await CreateLinkAsync(
                    AccountActionTokenUserType.Customer,
                    AccountActionTokenPurpose.PasswordReset,
                    customer.IdUsuarioCliente,
                    customer.Email,
                    "/restablecer-password",
                    cancellationToken);

                await SendPasswordResetEmailAsync(customer.Email, link, cancellationToken);
            }
        }

        /// <summary>
        /// Cambia la contraseña de una cuenta usando un token de recuperación válido.
        /// </summary>
        public async Task ResetPasswordAsync(ResetPasswordDTO dto, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
                throw new ValidationException("Las contraseñas no coinciden.");

            var actionToken = await GetOpenTokenAsync(
                dto.Token,
                AccountActionTokenPurpose.PasswordReset,
                cancellationToken);

            if (actionToken == null)
                throw new ValidationException("El enlace no es válido o ya se ha usado.");

            if (actionToken.ExpiresAt <= DateTime.UtcNow)
                throw new ValidationException("El enlace ha caducado.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
            var loweredEmail = actionToken.Email.ToLower();

            if (actionToken.UserType == AccountActionTokenUserType.Employee)
            {
                var employee = await _db.Empleados.FirstOrDefaultAsync(
                    empleado => empleado.Id == actionToken.UserId
                        && empleado.Activo
                        && empleado.Email.ToLower() == loweredEmail,
                    cancellationToken);

                if (employee == null)
                    throw new ValidationException("El enlace no es válido o ya se ha usado.");

                employee.Password = passwordHash;
                employee.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var customer = await _db.UsuariosCliente.FirstOrDefaultAsync(
                    cliente => cliente.IdUsuarioCliente == actionToken.UserId
                        && cliente.Activo
                        && cliente.Email.ToLower() == loweredEmail,
                    cancellationToken);

                if (customer == null)
                    throw new ValidationException("El enlace no es válido o ya se ha usado.");

                customer.PasswordHash = passwordHash;
                customer.UpdatedAt = DateTime.UtcNow;
            }

            actionToken.ConsumedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Envía un enlace de activación para un cliente pendiente de verificación.
        /// </summary>
        public async Task SendCustomerConfirmationEmailAsync(UsuarioCliente customer, CancellationToken cancellationToken = default)
        {
            if (customer.EmailVerificado)
                return;

            var link = await CreateLinkAsync(
                AccountActionTokenUserType.Customer,
                AccountActionTokenPurpose.EmailConfirmation,
                customer.IdUsuarioCliente,
                customer.Email,
                "/cuenta/confirmar-email",
                cancellationToken);

            await _emailService.SendAsync(
                customer.Email,
                "Activa tu cuenta de Gestaurante",
                $"Activa tu cuenta desde este enlace: {link}\n\nEl enlace caduca en 1 hora.",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Reenvía la confirmación de email si existe una cuenta cliente pendiente.
        /// </summary>
        public async Task ResendCustomerConfirmationEmailAsync(ResendConfirmationEmailDTO dto, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(dto.Email);
            var loweredEmail = email.ToLower();
            var customer = await _db.UsuariosCliente.FirstOrDefaultAsync(
                cliente => cliente.Activo
                    && !cliente.EmailVerificado
                    && cliente.Email.ToLower() == loweredEmail,
                cancellationToken);

            if (customer == null)
                return;

            await SendCustomerConfirmationEmailAsync(customer, cancellationToken);
        }

        /// <summary>
        /// Confirma el email de un cliente mediante un token de activación de un solo uso.
        /// </summary>
        public async Task ConfirmCustomerEmailAsync(ConfirmEmailByTokenDTO dto, CancellationToken cancellationToken = default)
        {
            var actionToken = await GetOpenTokenAsync(
                dto.Token,
                AccountActionTokenPurpose.EmailConfirmation,
                cancellationToken);

            if (actionToken == null || actionToken.UserType != AccountActionTokenUserType.Customer)
                throw new ValidationException("El enlace no es válido o ya se ha usado.");

            if (actionToken.ExpiresAt <= DateTime.UtcNow)
                throw new ValidationException("El enlace ha caducado.");

            var loweredEmail = actionToken.Email.ToLower();
            var customer = await _db.UsuariosCliente.FirstOrDefaultAsync(
                cliente => cliente.IdUsuarioCliente == actionToken.UserId
                    && cliente.Activo
                    && cliente.Email.ToLower() == loweredEmail,
                cancellationToken);

            if (customer == null)
                throw new ValidationException("El enlace no es válido o ya se ha usado.");

            customer.EmailVerificado = true;
            customer.UpdatedAt = DateTime.UtcNow;
            actionToken.ConsumedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> CreateLinkAsync(
            AccountActionTokenUserType userType,
            AccountActionTokenPurpose purpose,
            Guid userId,
            string email,
            string path,
            CancellationToken cancellationToken)
        {
            var rawToken = GenerateToken();
            var tokenHash = HashToken(rawToken);
            var now = DateTime.UtcNow;

            var existingTokens = await _db.AccountActionTokens
                .Where(token => token.UserType == userType
                    && token.Purpose == purpose
                    && token.UserId == userId
                    && token.ConsumedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var existingToken in existingTokens)
                existingToken.ConsumedAt = now;

            await _db.AccountActionTokens.AddAsync(new AccountActionToken
            {
                IdAccountActionToken = Guid.NewGuid(),
                UserType = userType,
                Purpose = purpose,
                UserId = userId,
                Email = NormalizeEmail(email),
                TokenHash = tokenHash,
                ExpiresAt = now.Add(TokenLifetime),
                CreatedAt = now
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return BuildFrontendLink(path, rawToken);
        }

        private async Task<AccountActionToken?> GetOpenTokenAsync(
            string rawToken,
            AccountActionTokenPurpose purpose,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return null;

            var tokenHash = HashToken(rawToken.Trim());
            return await _db.AccountActionTokens
                .FirstOrDefaultAsync(token => token.Purpose == purpose
                    && token.TokenHash == tokenHash
                    && token.ConsumedAt == null,
                    cancellationToken);
        }

        private async Task SendPasswordResetEmailAsync(string email, string link, CancellationToken cancellationToken)
        {
            await _emailService.SendAsync(
                email,
                "Restablece tu contraseña de Gestaurante",
                $"Hemos recibido una solicitud para restablecer tu contraseña.\n\nUsa este enlace: {link}\n\nEl enlace caduca en 1 hora. Si no has solicitado este cambio, puedes ignorar este correo.",
                cancellationToken: cancellationToken);
        }

        private string BuildFrontendLink(string path, string token)
        {
            var publicUrl = _frontendOptions.PublicUrl.Trim().TrimEnd('/');
            return $"{publicUrl}{path}?token={Uri.EscapeDataString(token)}";
        }

        private static string NormalizeEmail(string value)
        {
            var normalized = value.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("El email es obligatorio.");

            return normalized.Length <= 100 ? normalized : normalized[..100];
        }

        private static string GenerateToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }
    }
}
