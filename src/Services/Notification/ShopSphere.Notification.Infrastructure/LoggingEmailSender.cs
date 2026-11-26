using Microsoft.Extensions.Logging;
using ShopSphere.Notification.Domain.Notifications;

namespace ShopSphere.Notification.Infrastructure;

/// <summary>Default email adapter: writes the email to the log. Swap for SMTP/SendGrid in production.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(Guid userId, string subject, string body, CancellationToken ct)
    {
        _logger.LogInformation("EMAIL to user {UserId}: [{Subject}] {Body}", userId, subject, body);
        return Task.CompletedTask;
    }
}
