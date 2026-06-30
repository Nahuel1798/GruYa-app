using GruYaApi.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;

namespace GruYaApi.Service;

public interface INotificationService
{
    /// <summary>
    /// Sends a data-only FCM message (no notification field) so foreground and background handling is identical.
    /// The caller must include ["title"] and ["body"] inside the data dictionary if visible text is needed.
    /// </summary>
    Task SendToUserAsync(int userId, string? title = null, string? body = null, Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends a data-only FCM message to multiple tokens.
    /// The caller must include ["title"] and ["body"] inside the data dictionary if visible text is needed.
    /// </summary>
    Task SendToMultipleAsync(List<string> tokens, string? title = null, string? body = null, Dictionary<string, string>? data = null);
}

public class NotificationService : INotificationService
{
    private readonly DataContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DataContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        int userId,
        string? title = null,
        string? body = null,
        Dictionary<string, string>? data = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FcmToken is null or "")
        {
            _logger.LogWarning("User {UserId} has no FCM token, skipping notification", userId);
            return;
        }

        await SendSingleAsync(user.FcmToken, title, body, data);
    }

    public async Task SendToMultipleAsync(
        List<string> tokens,
        string? title = null,
        string? body = null,
        Dictionary<string, string>? data = null)
    {
        var validTokens = tokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        if (validTokens.Count == 0)
        {
            _logger.LogWarning("No valid FCM tokens to send to, skipping multicast");
            return;
        }

        var tasks = validTokens.Select(token => SendSingleAsync(token, title, body, data));
        await Task.WhenAll(tasks);
    }

    private async Task SendSingleAsync(
        string token,
        string? title,
        string? body,
        Dictionary<string, string>? data)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                },
            };

            var messaging = FirebaseMessaging.DefaultInstance;
            await messaging.SendAsync(message);
        }
        catch (Exception ex)
        {
            // Never log FCM tokens — they are device credentials
            _logger.LogWarning(ex, "Failed to send FCM notification (data-only)");
        }
    }
}
