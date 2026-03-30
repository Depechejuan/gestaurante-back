using System.Security.Cryptography;
using System.Text;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class CustomerAccountService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ICustomerJwtService _customerJwtService;
        private readonly MockPaymentService _mockPaymentService;

        public CustomerAccountService(
            AppDbContext db,
            IEmailService emailService,
            ICustomerJwtService customerJwtService,
            MockPaymentService mockPaymentService)
        {
            _db = db;
            _emailService = emailService;
            _customerJwtService = customerJwtService;
            _mockPaymentService = mockPaymentService;
        }

        public async Task<ClienteRegisterResponseDTO> RegisterAsync(ClienteRegisterDTO dto, CancellationToken cancellationToken = default)
        {
            var existing = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("Ya existe una cuenta de cliente con ese email.");

            var user = new UsuarioCliente
            {
                IdUsuarioCliente = Guid.NewGuid(),
                Email = dto.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Phone = dto.Phone.Trim(),
                Activo = true,
                EmailVerificado = false
            };

            await _db.UsuariosCliente.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await GenerateAndSendVerificationCodeAsync(user, cancellationToken);

            return new ClienteRegisterResponseDTO
            {
                IdUsuarioCliente = user.IdUsuarioCliente,
                Email = user.Email,
                EmailVerificado = user.EmailVerificado,
                Message = "Cuenta creada. Revisa tu correo para validar el email."
            };
        }

        public async Task VerifyEmailAsync(ClienteVerifyEmailDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower(), cancellationToken)
                ?? throw new KeyNotFoundException("Cuenta no encontrada.");

            var verification = await _db.ClienteEmailVerifications
                .Where(v => v.IdUsuarioCliente == user.IdUsuarioCliente && v.ConsumedAt == null)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No hay un código de validación activo.");

            if (verification.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("El código ha expirado.");

            verification.AttemptCount += 1;
            if (verification.CodeHash != HashCode(dto.Code))
            {
                await _db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Código de validación incorrecto.");
            }

            verification.ConsumedAt = DateTime.UtcNow;
            user.EmailVerificado = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ResendVerificationCodeAsync(ClienteResendCodeDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower(), cancellationToken)
                ?? throw new KeyNotFoundException("Cuenta no encontrada.");

            if (user.EmailVerificado)
                throw new InvalidOperationException("La cuenta ya tiene el email validado.");

            await GenerateAndSendVerificationCodeAsync(user, cancellationToken);
        }

        public async Task<ClienteTokenDTO> LoginAsync(ClienteLoginDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);
            if (user == null || !user.Activo || !user.EmailVerificado || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales de cliente inválidas.");

            var profile = MapProfile(user);
            var token = _customerJwtService.GenerarToken(profile);
            return new ClienteTokenDTO(token, _customerJwtService.GetExpiracion(), user.IdUsuarioCliente, user.Email, user.EmailVerificado);
        }

        public async Task<ClienteProfileDTO?> GetProfileAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            return user == null ? null : MapProfile(user);
        }

        public async Task<ClienteProfileDTO?> UpdateProfileAsync(Guid clienteId, UpdateClienteProfileDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            if (user == null)
                return null;

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Phone = dto.Phone.Trim();
            user.FiscalName = dto.FiscalName.Trim();
            user.Dni = dto.Dni.Trim().ToUpperInvariant();
            user.Cif = dto.Cif.Trim().ToUpperInvariant();
            user.BillingStreet = dto.BillingStreet.Trim();
            user.BillingCity = dto.BillingCity.Trim();
            user.BillingProvince = dto.BillingProvince.Trim();
            user.BillingPostalCode = dto.BillingPostalCode.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapProfile(user);
        }

        public async Task<List<ClienteDireccionDTO>> GetDireccionesAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            return await _db.ClienteDirecciones
                .AsNoTracking()
                .Where(d => d.IdUsuarioCliente == clienteId)
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Alias)
                .Select(MapDireccionExpression())
                .ToListAsync(cancellationToken);
        }

        public async Task<ClienteDireccionDTO> CreateDireccionAsync(Guid clienteId, CreateClienteDireccionDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto.IsDefault)
                await ClearDefaultAddressAsync(clienteId, cancellationToken);

            var direccion = new ClienteDireccion
            {
                IdClienteDireccion = Guid.NewGuid(),
                IdUsuarioCliente = clienteId,
                Alias = dto.Alias.Trim(),
                Street = dto.Street.Trim(),
                City = dto.City.Trim(),
                Province = dto.Province.Trim(),
                PostalCode = dto.PostalCode.Trim(),
                Notes = dto.Notes?.Trim() ?? string.Empty,
                IsDefault = dto.IsDefault
            };

            await _db.ClienteDirecciones.AddAsync(direccion, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return MapDireccion(direccion);
        }

        public async Task<ClienteDireccionDTO?> UpdateDireccionAsync(Guid clienteId, Guid direccionId, UpdateClienteDireccionDTO dto, CancellationToken cancellationToken = default)
        {
            var direccion = await _db.ClienteDirecciones
                .FirstOrDefaultAsync(d => d.IdClienteDireccion == direccionId && d.IdUsuarioCliente == clienteId, cancellationToken);
            if (direccion == null)
                return null;

            if (dto.IsDefault)
                await ClearDefaultAddressAsync(clienteId, cancellationToken);

            direccion.Alias = dto.Alias.Trim();
            direccion.Street = dto.Street.Trim();
            direccion.City = dto.City.Trim();
            direccion.Province = dto.Province.Trim();
            direccion.PostalCode = dto.PostalCode.Trim();
            direccion.Notes = dto.Notes?.Trim() ?? string.Empty;
            direccion.IsDefault = dto.IsDefault;
            direccion.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapDireccion(direccion);
        }

        public async Task DeleteDireccionAsync(Guid clienteId, Guid direccionId, CancellationToken cancellationToken = default)
        {
            var direccion = await _db.ClienteDirecciones
                .FirstOrDefaultAsync(d => d.IdClienteDireccion == direccionId && d.IdUsuarioCliente == clienteId, cancellationToken);
            if (direccion == null)
                throw new KeyNotFoundException("Dirección no encontrada.");

            _db.ClienteDirecciones.Remove(direccion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<ClienteMetodoPagoDTO>> GetMetodosPagoAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            return await _db.ClienteMetodosPago
                .AsNoTracking()
                .Where(m => m.IdUsuarioCliente == clienteId)
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.Brand)
                .Select(m => new ClienteMetodoPagoDTO
                {
                    IdClienteMetodoPago = m.IdClienteMetodoPago,
                    Brand = m.Brand,
                    Last4 = m.Last4,
                    HolderName = m.HolderName,
                    ExpMonth = m.ExpMonth,
                    ExpYear = m.ExpYear,
                    IsDefault = m.IsDefault
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ClienteMetodoPagoDTO> CreateMetodoPagoAsync(Guid clienteId, CreateClienteMetodoPagoDTO dto, CancellationToken cancellationToken = default)
        {
            var method = await _mockPaymentService.CreateSavedMethodAsync(clienteId, dto, cancellationToken);
            return MapMetodoPago(method);
        }

        public async Task DeleteMetodoPagoAsync(Guid clienteId, Guid paymentMethodId, CancellationToken cancellationToken = default)
        {
            await _mockPaymentService.DeleteSavedMethodAsync(clienteId, paymentMethodId, cancellationToken);
        }

        private async Task GenerateAndSendVerificationCodeAsync(UsuarioCliente user, CancellationToken cancellationToken)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var verification = new ClienteEmailVerification
            {
                IdClienteEmailVerification = Guid.NewGuid(),
                IdUsuarioCliente = user.IdUsuarioCliente,
                CodeHash = HashCode(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            await _db.ClienteEmailVerifications.AddAsync(verification, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await _emailService.SendAsync(
                user.Email,
                "Código de validación de Gestaurante",
                $"Tu código de validación es {code}. Caduca en 15 minutos.",
                cancellationToken);
        }

        private async Task ClearDefaultAddressAsync(Guid clienteId, CancellationToken cancellationToken)
        {
            var defaults = await _db.ClienteDirecciones
                .Where(d => d.IdUsuarioCliente == clienteId && d.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var address in defaults)
            {
                address.IsDefault = false;
            }
        }

        private static string HashCode(string code)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static ClienteProfileDTO MapProfile(UsuarioCliente user)
        {
            return new ClienteProfileDTO
            {
                IdUsuarioCliente = user.IdUsuarioCliente,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                FiscalName = user.FiscalName,
                Dni = user.Dni,
                Cif = user.Cif,
                BillingStreet = user.BillingStreet,
                BillingCity = user.BillingCity,
                BillingProvince = user.BillingProvince,
                BillingPostalCode = user.BillingPostalCode,
                Activo = user.Activo,
                EmailVerificado = user.EmailVerificado
            };
        }

        private static ClienteDireccionDTO MapDireccion(ClienteDireccion direccion)
        {
            return new ClienteDireccionDTO
            {
                IdClienteDireccion = direccion.IdClienteDireccion,
                Alias = direccion.Alias,
                Street = direccion.Street,
                City = direccion.City,
                Province = direccion.Province,
                PostalCode = direccion.PostalCode,
                Notes = direccion.Notes,
                IsDefault = direccion.IsDefault
            };
        }

        private static System.Linq.Expressions.Expression<Func<ClienteDireccion, ClienteDireccionDTO>> MapDireccionExpression()
        {
            return direccion => new ClienteDireccionDTO
            {
                IdClienteDireccion = direccion.IdClienteDireccion,
                Alias = direccion.Alias,
                Street = direccion.Street,
                City = direccion.City,
                Province = direccion.Province,
                PostalCode = direccion.PostalCode,
                Notes = direccion.Notes,
                IsDefault = direccion.IsDefault
            };
        }

        private static ClienteMetodoPagoDTO MapMetodoPago(ClienteMetodoPago method)
        {
            return new ClienteMetodoPagoDTO
            {
                IdClienteMetodoPago = method.IdClienteMetodoPago,
                Brand = method.Brand,
                Last4 = method.Last4,
                HolderName = method.HolderName,
                ExpMonth = method.ExpMonth,
                ExpYear = method.ExpYear,
                IsDefault = method.IsDefault
            };
        }
    }
}
