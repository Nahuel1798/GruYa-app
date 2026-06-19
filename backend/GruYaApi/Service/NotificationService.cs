using GruYaApi.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;

namespace GruYaApi.Service;

public interface INotificationService
{
    Task SendToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null);

    Task SendToMultipleAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null);
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
        string title,
        string body,
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
        string title,
        string body,
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
        string title,
        string body,
        Dictionary<string, string>? data)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
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
            _logger.LogWarning(ex, "Failed to send FCM notification '{Title}'", title);
        }
    }
}
