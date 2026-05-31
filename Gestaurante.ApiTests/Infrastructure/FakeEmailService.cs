using Gestaurante.Models.Services;

namespace Gestaurante.ApiTests.Infrastructure;

public sealed class FakeEmailService : IEmailService
{
    private readonly List<SentEmail> _messages = new();

    public IReadOnlyList<SentEmail> Messages => _messages;

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default,
        string? replyToEmail = null,
        string? replyToName = null)
    {
        _messages.Add(new SentEmail(toEmail, subject, body, isHtml, replyToEmail, replyToName));
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _messages.Clear();
    }

    public sealed record SentEmail(
        string ToEmail,
        string Subject,
        string Body,
        bool IsHtml,
        string? ReplyToEmail,
        string? ReplyToName);
}
