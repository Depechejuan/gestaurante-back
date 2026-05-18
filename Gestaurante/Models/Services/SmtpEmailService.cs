using Gestaurante.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host!, _options.Port!.Value, SecureSocketOptions.Auto, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.User) && !string.IsNullOrWhiteSpace(_options.Password))
                await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
