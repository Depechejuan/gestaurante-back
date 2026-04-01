using System.Net;
using System.Net.Mail;

namespace Gestaurante.Models.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly string? _host;
        private readonly string? _port;
        private readonly string? _user;
        private readonly string? _password;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SmtpEmailService(ILogger<SmtpEmailService> logger)
        {
            _logger = logger;
            _host = Environment.GetEnvironmentVariable("SMTP_HOST");
            _port = Environment.GetEnvironmentVariable("SMTP_PORT");
            _user = Environment.GetEnvironmentVariable("SMTP_USER");
            _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            _fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "no-reply@gestaurante.local";
            _fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Gestaurante";
        }

        public async Task SendAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_port))
            {
                _logger.LogWarning("SMTP no configurado. Se omite el envío real a {Email}. Asunto: {Subject}", toEmail, subject);
                return;
            }

            using var client = new SmtpClient(_host, int.Parse(_port))
            {
                EnableSsl = true
            };

            if (!string.IsNullOrWhiteSpace(_user) && !string.IsNullOrWhiteSpace(_password))
                client.Credentials = new NetworkCredential(_user, _password);

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
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
