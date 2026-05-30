using System.ComponentModel.DataAnnotations;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    /// <summary>
    /// Gestiona el ciclo de vida de la cuenta de cliente, su perfil y sus datos auxiliares.
    /// </summary>
    public class CustomerAccountService
    {
        private readonly AppDbContext _db;
        private readonly ICustomerJwtService _customerJwtService;
        private readonly SimulatedPaymentService _simulatedPaymentService;
        private readonly AccountActionTokenService _accountActionTokenService;

        /// <summary>
        /// Inicializa el servicio de cuentas de cliente.
        /// </summary>
        /// <param name="db">Contexto EF del dominio.</param>
        /// <param name="customerJwtService">Servicio emisor del JWT de cliente.</param>
        /// <param name="simulatedPaymentService">Servicio de métodos de pago simulados.</param>
        /// <param name="accountActionTokenService">Servicio de enlaces de activación y recuperación.</param>
        public CustomerAccountService(
            AppDbContext db,
            ICustomerJwtService customerJwtService,
            SimulatedPaymentService simulatedPaymentService,
            AccountActionTokenService accountActionTokenService)
        {
            _db = db;
            _customerJwtService = customerJwtService;
            _simulatedPaymentService = simulatedPaymentService;
            _accountActionTokenService = accountActionTokenService;
        }

        /// <summary>
        /// Registra una nueva cuenta de cliente y envía un enlace de verificación por correo.
        /// </summary>
        /// <param name="dto">Datos mínimos de alta del cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Resultado del registro con el identificador del cliente creado.</returns>
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
                FirstName = ResolveDefaultCustomerName(dto.Email),
                LastName = string.Empty,
                Phone = string.Empty,
                Activo = true,
                EmailVerificado = false
            };

            await _db.UsuariosCliente.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await _accountActionTokenService.SendCustomerConfirmationEmailAsync(user, cancellationToken);

            return new ClienteRegisterResponseDTO
            {
                IdUsuarioCliente = user.IdUsuarioCliente,
                Email = user.Email,
                EmailVerificado = user.EmailVerificado,
                Message = "Cuenta creada. Revisa tu correo para activar la cuenta."
            };
        }

        /// <summary>
        /// Verifica el correo electrónico de una cuenta usando un enlace de activación.
        /// </summary>
        /// <param name="dto">Token de activación recibido desde el email.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        public Task ConfirmEmailAsync(ConfirmEmailByTokenDTO dto, CancellationToken cancellationToken = default)
        {
            return _accountActionTokenService.ConfirmCustomerEmailAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Regenera y reenvía un enlace de verificación para una cuenta todavía no validada.
        /// </summary>
        /// <param name="dto">Datos de la cuenta para la que se solicita un nuevo enlace.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        public Task ResendConfirmationEmailAsync(ResendConfirmationEmailDTO dto, CancellationToken cancellationToken = default)
        {
            return _accountActionTokenService.ResendCustomerConfirmationEmailAsync(dto, cancellationToken);
        }

        /// <summary>
        /// Autentica a un cliente verificado y devuelve su token de acceso.
        /// </summary>
        /// <param name="dto">Credenciales de acceso del cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Token de cliente junto a sus datos mínimos de sesión.</returns>
        public async Task<ClienteTokenDTO?> LoginAsync(ClienteLoginDTO dto, CancellationToken cancellationToken = default)
        {
            var email = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
            if (user == null || !user.Activo || !user.EmailVerificado || !VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            var profile = MapProfile(user);
            var token = _customerJwtService.GenerarToken(profile);
            return new ClienteTokenDTO(token, _customerJwtService.GetExpiracion(), user.IdUsuarioCliente, user.Email, user.EmailVerificado);
        }

        /// <summary>
        /// Recupera el perfil del cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Perfil del cliente o <see langword="null"/> si no existe.</returns>
        public async Task<ClienteProfileDTO?> GetProfileAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            return user == null ? null : MapProfile(user);
        }

        /// <summary>
        /// Recupera clientes para uso interno con filtro opcional por texto libre.
        /// </summary>
        /// <param name="query">Texto opcional de búsqueda por identidad o datos fiscales.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Lista de clientes visibles para el panel interno.</returns>
        public async Task<List<ClienteProfileDTO>> GetInternalClientesAsync(string? query, CancellationToken cancellationToken = default)
        {
            var clientesQuery = _db.UsuariosCliente.AsNoTracking();
            var term = query?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(term))
                clientesQuery = clientesQuery.Where(user =>
                    user.Email.ToLower().Contains(term)
                    || user.FirstName.ToLower().Contains(term)
                    || user.LastName.ToLower().Contains(term)
                    || user.FiscalName.ToLower().Contains(term)
                    || user.Dni.ToLower().Contains(term)
                    || user.Cif.ToLower().Contains(term));

            var users = await clientesQuery
                .OrderByDescending(user => user.CreatedAt)
                .ThenBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToListAsync(cancellationToken);

            return users.Select(MapProfile).ToList();
        }

        /// <summary>
        /// Crea un cliente desde el panel interno dejando la cuenta activa y validada.
        /// </summary>
        /// <param name="dto">Datos fiscales y de contacto del cliente interno.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Perfil del cliente creado.</returns>
        public async Task<ClienteProfileDTO> CreateInternalClienteAsync(CreateInternalClienteDTO dto, CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(dto.Email);
            if (await _db.UsuariosCliente.AnyAsync(user => user.Email.ToLower() == email.ToLower(), cancellationToken))
                throw new InvalidOperationException("Ya existe un cliente con ese email.");

            var fiscalName = NormalizeText(dto.FiscalName, 160);
            var firstName = string.IsNullOrWhiteSpace(dto.FirstName)
                ? ResolveFirstNameFromFiscalName(fiscalName)
                : NormalizeText(dto.FirstName, 120);
            var lastName = string.IsNullOrWhiteSpace(dto.LastName)
                ? ResolveLastNameFromFiscalName(fiscalName)
                : NormalizeText(dto.LastName, 160);

            var user = new UsuarioCliente
            {
                IdUsuarioCliente = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                FirstName = firstName,
                LastName = lastName,
                Phone = NormalizeText(dto.Phone, 25),
                FiscalName = fiscalName,
                Dni = NormalizeUpperText(dto.Dni, 15),
                Cif = NormalizeUpperText(dto.Cif, 20),
                BillingStreet = NormalizeText(dto.BillingStreet, 200),
                BillingCity = NormalizeText(dto.BillingCity, 120),
                BillingProvince = NormalizeText(dto.BillingProvince, 120),
                BillingPostalCode = NormalizeText(dto.BillingPostalCode, 20),
                Activo = true,
                EmailVerificado = true,
                CreatedAt = DateTime.UtcNow
            };

            await _db.UsuariosCliente.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return MapProfile(user);
        }

        /// <summary>
        /// Actualiza los datos globales de un cliente desde el panel interno.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente a modificar.</param>
        /// <param name="dto">Datos actualizados del cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Perfil actualizado o <see langword="null"/> si el cliente no existe.</returns>
        public async Task<ClienteProfileDTO?> UpdateInternalClienteAsync(Guid clienteId, UpdateInternalClienteDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            if (user == null)
                return null;

            var email = NormalizeEmail(dto.Email);
            var emailExists = await _db.UsuariosCliente.AnyAsync(
                existingUser => existingUser.IdUsuarioCliente != clienteId && existingUser.Email.ToLower() == email.ToLower(),
                cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("Ya existe un cliente con ese email.");

            var fiscalName = NormalizeText(dto.FiscalName, 160);
            user.Email = email;
            user.FiscalName = fiscalName;
            user.FirstName = string.IsNullOrWhiteSpace(dto.FirstName)
                ? ResolveFirstNameFromFiscalName(fiscalName)
                : NormalizeText(dto.FirstName, 120);
            user.LastName = string.IsNullOrWhiteSpace(dto.LastName)
                ? ResolveLastNameFromFiscalName(fiscalName)
                : NormalizeText(dto.LastName, 160);
            user.Phone = NormalizeText(dto.Phone, 25);
            user.Dni = NormalizeUpperText(dto.Dni, 15);
            user.Cif = NormalizeUpperText(dto.Cif, 20);
            user.BillingStreet = NormalizeText(dto.BillingStreet, 200);
            user.BillingCity = NormalizeText(dto.BillingCity, 120);
            user.BillingProvince = NormalizeText(dto.BillingProvince, 120);
            user.BillingPostalCode = NormalizeText(dto.BillingPostalCode, 20);
            if (dto.Activo.HasValue)
                user.Activo = dto.Activo.Value;

            if (dto.EmailVerificado.HasValue)
                user.EmailVerificado = dto.EmailVerificado.Value;

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapProfile(user);
        }

        /// <summary>
        /// Activa o desactiva una cuenta de cliente sin eliminarla.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente.</param>
        /// <param name="activo">Nuevo estado activo de la cuenta.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Perfil actualizado o <see langword="null"/> si el cliente no existe.</returns>
        public async Task<ClienteProfileDTO?> SetActivoAsync(Guid clienteId, bool activo, CancellationToken cancellationToken = default)
        {
            var user = await _db.UsuariosCliente.FirstOrDefaultAsync(u => u.IdUsuarioCliente == clienteId, cancellationToken);
            if (user == null)
                return null;

            user.Activo = activo;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapProfile(user);
        }

        /// <summary>
        /// Actualiza el perfil editable del propio cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="dto">Datos de perfil editables por el propio cliente.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Perfil actualizado o <see langword="null"/> si no existe.</returns>
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

        /// <summary>
        /// Recupera las direcciones guardadas del cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Direcciones guardadas del cliente.</returns>
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

        /// <summary>
        /// Añade una nueva dirección al perfil del cliente.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="dto">Datos de la nueva dirección.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Dirección creada.</returns>
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

        /// <summary>
        /// Modifica una dirección existente del cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="direccionId">Identificador de la dirección a modificar.</param>
        /// <param name="dto">Datos actualizados de la dirección.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Dirección actualizada o <see langword="null"/> si no existe.</returns>
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

        /// <summary>
        /// Elimina una dirección guardada del cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="direccionId">Identificador de la dirección a borrar.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        public async Task DeleteDireccionAsync(Guid clienteId, Guid direccionId, CancellationToken cancellationToken = default)
        {
            var direccion = await _db.ClienteDirecciones
                .FirstOrDefaultAsync(d => d.IdClienteDireccion == direccionId && d.IdUsuarioCliente == clienteId, cancellationToken);
            if (direccion == null)
                throw new KeyNotFoundException("Dirección no encontrada.");

            _db.ClienteDirecciones.Remove(direccion);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Recupera los métodos de pago guardados del cliente autenticado.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Métodos de pago disponibles para el cliente.</returns>
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

        /// <summary>
        /// Crea y guarda un nuevo método de pago simulado para reutilizarlo en futuros pedidos.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="dto">Datos del método de pago a guardar.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        /// <returns>Método de pago guardado ya proyectado al DTO público.</returns>
        public async Task<ClienteMetodoPagoDTO> CreateMetodoPagoAsync(Guid clienteId, CreateClienteMetodoPagoDTO dto, CancellationToken cancellationToken = default)
        {
            var method = await _simulatedPaymentService.CreateSavedMethodAsync(clienteId, dto, cancellationToken);
            return MapMetodoPago(method);
        }

        /// <summary>
        /// Elimina un método de pago previamente guardado por el cliente.
        /// </summary>
        /// <param name="clienteId">Identificador del cliente autenticado.</param>
        /// <param name="paymentMethodId">Identificador del método de pago.</param>
        /// <param name="cancellationToken">Token de cancelación de la operación.</param>
        public async Task DeleteMetodoPagoAsync(Guid clienteId, Guid paymentMethodId, CancellationToken cancellationToken = default)
        {
            await _simulatedPaymentService.DeleteSavedMethodAsync(clienteId, paymentMethodId, cancellationToken);
        }

        /// <summary>
        /// Retira la marca de dirección por defecto al resto de direcciones del cliente.
        /// </summary>
        private async Task ClearDefaultAddressAsync(Guid clienteId, CancellationToken cancellationToken)
        {
            var defaults = await _db.ClienteDirecciones
                .Where(d => d.IdUsuarioCliente == clienteId && d.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var address in defaults)
                address.IsDefault = false;
        }

        /// <summary>
        /// Verifica la contraseÃ±a evitando que un hash corrupto rompa el login.
        /// </summary>
        private static bool VerifyPassword(string password, string? passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Deriva un nombre básico a partir del email cuando aún no existe información personal.
        /// </summary>
        private static string ResolveDefaultCustomerName(string email)
        {
            var localPart = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(localPart))
                return "Cliente";

            var sanitized = localPart.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ').Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "Cliente" : sanitized;
        }

        /// <summary>
        /// Obtiene un nombre provisional a partir del nombre fiscal.
        /// </summary>
        private static string ResolveFirstNameFromFiscalName(string fiscalName)
        {
            var sanitized = fiscalName.Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
                return "Cliente";

            return sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Cliente";
        }

        /// <summary>
        /// Obtiene apellidos provisionales a partir del nombre fiscal.
        /// </summary>
        private static string ResolveLastNameFromFiscalName(string fiscalName)
        {
            var sanitized = fiscalName.Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
                return string.Empty;

            var parts = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
        }

        /// <summary>
        /// Mapea la entidad de cliente al DTO de perfil expuesto por la API.
        /// </summary>
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

        /// <summary>
        /// Mapea una dirección guardada al DTO consumido por el front.
        /// </summary>
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

        /// <summary>
        /// Proporciona una proyección reutilizable de direcciones para consultas EF.
        /// </summary>
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

        /// <summary>
        /// Mapea un método de pago guardado al DTO público del área cliente.
        /// </summary>
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

        /// <summary>
        /// Normaliza y valida el email almacenado de un cliente.
        /// </summary>
        private static string NormalizeEmail(string value)
        {
            var normalized = value.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("El email es obligatorio.");

            return TrimToLength(normalized, 100);
        }

        /// <summary>
        /// Recorta un texto libre al tamaño máximo permitido.
        /// </summary>
        private static string NormalizeText(string value, int maxLength)
        {
            return TrimToLength(value.Trim(), maxLength);
        }

        /// <summary>
        /// Recorta un texto y lo transforma a mayúsculas para documentos fiscales.
        /// </summary>
        private static string NormalizeUpperText(string value, int maxLength)
        {
            return TrimToLength(value.Trim().ToUpperInvariant(), maxLength);
        }

        /// <summary>
        /// Limita un texto al tamaño máximo admitido por el dominio.
        /// </summary>
        private static string TrimToLength(string value, int maxLength)
        {
            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}
