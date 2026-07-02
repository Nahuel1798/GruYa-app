using GruYaApi.Data;
using GruYaApi.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using System.Globalization;
using System.Text.Json;

namespace GruYaApi.Service;

public class NotificationService : INotificationService
{
    private readonly DataContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DataContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Assistance lifecycle ──────────────────────────────────────────────

    public async Task NotifyDirectedAssistanceToProviderAsync(int providerUserId, int assistanceId, string serviceType, string issueType)
    {
        var payload = new NotificationPayload(
            Type: "directed_assistance",
            Title: "Te han solicitado un servicio",
            Body: $"{serviceType} - {issueType}",
            AssistanceId: assistanceId,
            ServiceType: serviceType,
            IssueType: issueType
        );
        await PersistAndSendAsync(providerUserId, payload);
    }

    public async Task NotifyNewAssistanceToProvidersAsync(Dictionary<int, string> recipientTokens, int assistanceId, string serviceType, string issueType, decimal originLat, decimal originLon)
    {
        var payload = new NotificationPayload(
            Type: "new_assistance",
            Title: "Nueva solicitud de auxilio cerca",
            Body: $"Tipo: {serviceType}",
            AssistanceId: assistanceId,
            ServiceType: serviceType,
            IssueType: issueType,
            OriginLat: originLat,
            OriginLon: originLon
        );
        await PersistAndSendMulticastAsync(recipientTokens, payload);
    }

    public async Task NotifyTripStartedToClientAsync(int clientUserId, int assistanceId, int providerId, string trackingSessionId)
    {
        var payload = new NotificationPayload(
            Type: "trip_started",
            Title: "Tu proveedor ha iniciado el viaje",
            Body: "El proveedor está en camino hacia tu ubicación",
            AssistanceId: assistanceId,
            ProviderId: providerId,
            TrackingSessionId: trackingSessionId
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    public async Task NotifyProviderArrivedToClientAsync(int clientUserId, int assistanceId, int providerId)
    {
        var payload = new NotificationPayload(
            Type: "provider.arrived",
            Title: "El proveedor llegó a tu ubicación",
            Body: "El proveedor está en tu ubicación",
            AssistanceId: assistanceId,
            ProviderId: providerId
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    public async Task NotifyProviderHeadingToDestinationToClientAsync(int clientUserId, int assistanceId, int providerId)
    {
        var payload = new NotificationPayload(
            Type: "provider.heading_to_destination",
            Title: "El proveedor se dirige hacia tu destino",
            Body: "El proveedor está en camino a tu destino",
            AssistanceId: assistanceId,
            ProviderId: providerId
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    public async Task NotifyServiceCompletedToClientAsync(int clientUserId, int assistanceId, int providerId)
    {
        var payload = new NotificationPayload(
            Type: "provider.service_completed",
            Title: "El servicio fue completado",
            Body: "Tu servicio de asistencia ha finalizado",
            AssistanceId: assistanceId,
            ProviderId: providerId
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    // ── Quote lifecycle ───────────────────────────────────────────────────

    public async Task NotifyNewQuoteToClientAsync(int clientUserId, int assistanceId, int quoteId, string providerName, decimal price)
    {
        var payload = new NotificationPayload(
            Type: "new_quote",
            Title: "Recibiste una cotización",
            Body: $"{providerName} cotizó ${price}",
            AssistanceId: assistanceId,
            QuoteId: quoteId,
            ProviderName: providerName,
            Price: price
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    public async Task NotifyQuoteAcceptedToProviderAsync(int providerUserId, int assistanceId, int providerProfileId)
    {
        var payload = new NotificationPayload(
            Type: "quote_accepted_provider",
            Title: "¡Servicio asignado!",
            Body: "Tu cotización fue aceptada",
            AssistanceId: assistanceId,
            ProviderProfileId: providerProfileId
        );
        await PersistAndSendAsync(providerUserId, payload);
    }

    public async Task NotifyQuoteAcceptedToClientAsync(int clientUserId, int assistanceId, string companyName)
    {
        var payload = new NotificationPayload(
            Type: "quote_accepted_client",
            Title: "Tu solicitud está siendo atendida",
            Body: $"{companyName} está en camino",
            AssistanceId: assistanceId,
            ProviderName: companyName
        );
        await PersistAndSendAsync(clientUserId, payload);
    }

    public async Task NotifyQuoteRejectedToProviderAsync(int providerUserId, int assistanceId)
    {
        var payload = new NotificationPayload(
            Type: "quote_rejected",
            Title: "Cotización rechazada",
            Body: "Tu cotización fue rechazada",
            AssistanceId: assistanceId
        );
        await PersistAndSendAsync(providerUserId, payload);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task PersistAndSendAsync(int userId, NotificationPayload payload)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            var token = user?.FcmToken;

            var notification = new Models.Notification
            {
                UserId = userId,
                AssistanceId = payload.AssistanceId,
                Type = payload.Type,
                Title = payload.Title,
                Body = payload.Body,
                DataJson = JsonSerializer.Serialize(payload),
                SentAt = DateTime.UtcNow,
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                await SendViaFcmAsync(token, payload);
            }
            else
            {
                _logger.LogWarning("User {UserId} has no FCM token, skipping FCM send", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist and send notification for user {UserId}", userId);
        }
    }

    private async Task PersistAndSendMulticastAsync(Dictionary<int, string> recipientTokens, NotificationPayload payload)
    {
        try
        {
            var validRecipients = recipientTokens
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .DistinctBy(kvp => kvp.Value)
                .ToList();

            if (validRecipients.Count == 0)
            {
                _logger.LogWarning("No valid FCM tokens to send to, skipping multicast");
                return;
            }

            var dataJson = JsonSerializer.Serialize(payload);
            var now = DateTime.UtcNow;

            foreach (var (userId, _) in validRecipients)
            {
                _context.Notifications.Add(new Models.Notification
                {
                    UserId = userId,
                    AssistanceId = payload.AssistanceId,
                    Type = payload.Type,
                    Title = payload.Title,
                    Body = payload.Body,
                    DataJson = dataJson,
                    SentAt = now,
                });
            }
            await _context.SaveChangesAsync();

            foreach (var (_, token) in validRecipients)
            {
                await SendViaFcmAsync(token, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist and send multicast notification");
        }
    }

    private async Task SendViaFcmAsync(string token, NotificationPayload payload)
    {
        try
        {
            var dict = new Dictionary<string, string>
            {
                ["type"] = payload.Type,
                ["assistanceId"] = payload.AssistanceId.ToString(),
                ["title"] = payload.Title,
                ["body"] = payload.Body,
            };

            if (payload.ProviderId.HasValue)
                dict["providerId"] = payload.ProviderId.Value.ToString();
            if (payload.TrackingSessionId is not null)
                dict["trackingSessionId"] = payload.TrackingSessionId;
            if (payload.QuoteId.HasValue)
                dict["quoteId"] = payload.QuoteId.Value.ToString();
            if (payload.ProviderName is not null)
                dict["providerName"] = payload.ProviderName;
            if (payload.Price.HasValue)
                dict["price"] = payload.Price.Value.ToString(CultureInfo.InvariantCulture);
            if (payload.ServiceType is not null)
                dict["serviceType"] = payload.ServiceType;
            if (payload.IssueType is not null)
                dict["issueType"] = payload.IssueType;
            if (payload.OriginLat.HasValue)
                dict["originLat"] = payload.OriginLat.Value.ToString(CultureInfo.InvariantCulture);
            if (payload.OriginLon.HasValue)
                dict["originLon"] = payload.OriginLon.Value.ToString(CultureInfo.InvariantCulture);
            if (payload.ProviderProfileId.HasValue)
                dict["providerProfileId"] = payload.ProviderProfileId.Value.ToString();

            var message = new Message
            {
                Token = token,
                Data = dict,
                Android = new AndroidConfig { Priority = Priority.High },
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send FCM notification");
        }
    }
}
