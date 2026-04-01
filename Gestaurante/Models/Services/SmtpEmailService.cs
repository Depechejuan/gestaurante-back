using System.Net;
using System.Net.Mail;
using Gestaurante.Configuration;
using Microsoft.Extensions.Options;

namespace Gestaurante.Models.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpOptions _options;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, IOptions<SmtpOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogWarning("SMTP no configurado. Se omite el envío real a {Email}. Asunto: {Subject}", toEmail, subject);
                return;
            }

            using var client = new SmtpClient(_options.Host!, _options.Port!.Value)
            {
                EnableSsl = true
            };

            if (!string.IsNullOrWhiteSpace(_options.User) && !string.IsNullOrWhiteSpace(_options.Password))
                client.Credentials = new NetworkCredential(_options.User, _options.Password);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(toEmail);

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
