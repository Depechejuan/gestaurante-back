using System.ComponentModel.DataAnnotations;
using Gestaurante.Configuration;
using Gestaurante.Models.DTO;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public class ContactService
    {
        private readonly IEmailService _emailService;
        private readonly ContactOptions _contactOptions;

        public ContactService(IEmailService emailService, IOptions<ContactOptions> contactOptions)
        {
            _emailService = emailService;
            _contactOptions = contactOptions.Value;
        }

        public async Task SendAsync(ContactMessageDTO dto, CancellationToken cancellationToken = default)
        {
            var name = CleanRequired(dto.Name, "El nombre es obligatorio.", 120);
            var email = CleanRequired(dto.Email, "El email es obligatorio.", 160);
            var phone = CleanOptional(dto.Phone, 40);
            var customSubject = CleanOptional(dto.Subject, 160);
            var message = CleanRequired(dto.Message, "El mensaje es obligatorio.", 2000);

            if (message.Length < 10)
                throw new ValidationException("El mensaje debe tener al menos 10 caracteres.");

            var subject = string.IsNullOrWhiteSpace(customSubject)
                ? $"Nuevo mensaje de contacto de {name}"
                : $"Contacto Gestaurante: {customSubject}";

            var body = $"""
                Nuevo mensaje desde el formulario de contacto de Gestaurante.

                Nombre: {name}
                Email: {email}
                Telefono: {ResolveDisplayValue(phone)}
                Fecha UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

                Mensaje:
                {message}
                """;

            await _emailService.SendAsync(
                _contactOptions.ToEmail,
                subject,
                body,
                cancellationToken: cancellationToken,
                replyToEmail: email,
                replyToName: name);
        }

        private static string CleanRequired(string value, string errorMessage, int maxLength)
        {
            var clean = CleanOptional(value, maxLength);
            if (string.IsNullOrWhiteSpace(clean))
                throw new ValidationException(errorMessage);

            return clean;
        }

        private static string CleanOptional(string? value, int maxLength)
        {
            var clean = value?.Trim() ?? string.Empty;
            if (clean.Length <= maxLength)
                return clean;

            return clean[..maxLength].TrimEnd();
        }

        private static string ResolveDisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "No indicado" : value;
        }
    }
}
