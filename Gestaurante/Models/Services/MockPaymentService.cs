using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Gestaurante.Models.Data;
using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Services
{
    public class MockPaymentService
    {
        private readonly AppDbContext _db;

        public MockPaymentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ClienteMetodoPago> CreateSavedMethodAsync(Guid clienteId, CreateClienteMetodoPagoDTO dto, CancellationToken cancellationToken = default)
        {
            ValidateCardData(dto.CardNumber, dto.ExpMonth, dto.ExpYear, dto.HolderName);

            if (dto.IsDefault)
                await ClearDefaultMethodAsync(clienteId, cancellationToken);

            var method = new ClienteMetodoPago
            {
                IdClienteMetodoPago = Guid.NewGuid(),
                IdUsuarioCliente = clienteId,
                MockPaymentToken = GenerateMockToken(dto.CardNumber, clienteId),
                Brand = DetectBrand(dto.CardNumber),
                Last4 = dto.CardNumber[^4..],
                HolderName = dto.HolderName.Trim(),
                ExpMonth = dto.ExpMonth,
                ExpYear = dto.ExpYear,
                IsDefault = dto.IsDefault
            };

            await _db.ClienteMetodosPago.AddAsync(method, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return method;
        }

        public async Task<ClienteMetodoPago> ResolvePaymentMethodAsync(Guid clienteId, CheckoutPaymentMethodDTO paymentMethod, CancellationToken cancellationToken = default)
        {
            if (paymentMethod.IdClienteMetodoPago.HasValue)
            {
                var existing = await _db.ClienteMetodosPago
                    .FirstOrDefaultAsync(m => m.IdClienteMetodoPago == paymentMethod.IdClienteMetodoPago.Value && m.IdUsuarioCliente == clienteId, cancellationToken);

                return existing ?? throw new KeyNotFoundException("Método de pago no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod.CardNumber)
                || string.IsNullOrWhiteSpace(paymentMethod.HolderName)
                || !paymentMethod.ExpMonth.HasValue
                || !paymentMethod.ExpYear.HasValue)
            {
                throw new ValidationException("Debes indicar una tarjeta válida para el pago online.");
            }

            var createDto = new CreateClienteMetodoPagoDTO
            {
                CardNumber = paymentMethod.CardNumber.Trim(),
                HolderName = paymentMethod.HolderName.Trim(),
                ExpMonth = paymentMethod.ExpMonth.Value,
                ExpYear = paymentMethod.ExpYear.Value,
                IsDefault = paymentMethod.SaveForFuture
            };

            if (paymentMethod.SaveForFuture)
                return await CreateSavedMethodAsync(clienteId, createDto, cancellationToken);

            ValidateCardData(createDto.CardNumber, createDto.ExpMonth, createDto.ExpYear, createDto.HolderName);

            return new ClienteMetodoPago
            {
                IdClienteMetodoPago = Guid.NewGuid(),
                IdUsuarioCliente = clienteId,
                MockPaymentToken = GenerateMockToken(createDto.CardNumber, clienteId),
                Brand = DetectBrand(createDto.CardNumber),
                Last4 = createDto.CardNumber[^4..],
                HolderName = createDto.HolderName,
                ExpMonth = createDto.ExpMonth,
                ExpYear = createDto.ExpYear,
                IsDefault = false
            };
        }

        public async Task DeleteSavedMethodAsync(Guid clienteId, Guid paymentMethodId, CancellationToken cancellationToken = default)
        {
            var method = await _db.ClienteMetodosPago
                .FirstOrDefaultAsync(m => m.IdClienteMetodoPago == paymentMethodId && m.IdUsuarioCliente == clienteId, cancellationToken);

            if (method == null)
                throw new KeyNotFoundException("Método de pago no encontrado.");

            _db.ClienteMetodosPago.Remove(method);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task ClearDefaultMethodAsync(Guid clienteId, CancellationToken cancellationToken)
        {
            var defaults = await _db.ClienteMetodosPago
                .Where(m => m.IdUsuarioCliente == clienteId && m.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var method in defaults)
                method.IsDefault = false;
        }

        private static void ValidateCardData(string cardNumber, int expMonth, int expYear, string holderName)
        {
            var normalized = new string(cardNumber.Where(char.IsDigit).ToArray());
            if (normalized.Length < 12 || normalized.Length > 19)
                throw new ValidationException("Número de tarjeta no válido.");

            if (string.IsNullOrWhiteSpace(holderName))
                throw new ValidationException("El titular de la tarjeta es obligatorio.");

            if (expMonth < 1 || expMonth > 12)
                throw new ValidationException("Mes de expiración no válido.");

            var currentYear = DateTime.UtcNow.Year;
            if (expYear < currentYear)
                throw new ValidationException("Año de expiración no válido.");
        }

        private static string GenerateMockToken(string cardNumber, Guid clienteId)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{clienteId}:{cardNumber}:{DateTime.UtcNow.Ticks}"));
            return $"mock_{Convert.ToHexString(bytes)[..24]}";
        }

        private static string DetectBrand(string cardNumber)
        {
            var normalized = new string(cardNumber.Where(char.IsDigit).ToArray());
            if (normalized.StartsWith("4"))
                return "VISA";

            if (normalized.StartsWith("5") || normalized.StartsWith("2"))
                return "MASTERCARD";

            if (normalized.StartsWith("34") || normalized.StartsWith("37"))
                return "AMEX";

            return "TARJETA";
        }
    }
}
