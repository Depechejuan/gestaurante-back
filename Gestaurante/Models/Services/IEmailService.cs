namespace Gestaurante.Models.Services
{
    public interface IEmailService
    {
        Task SendAsync(
            string toEmail,
            string subject,
            string body,
            bool isHtml = false,
            CancellationToken cancellationToken = default,
            string? replyToEmail = null,
            string? replyToName = null);
    }
}
