namespace GruYaApi.Service;

internal sealed record NotificationPayload(
    string Type,
    string Title,
    string Body,
    int AssistanceId,
    int? ProviderId = null,
    int? ProviderProfileId = null,
    string? TrackingSessionId = null,
    int? QuoteId = null,
    string? ProviderName = null,
    decimal? Price = null,
    string? ServiceType = null,
    string? IssueType = null,
    decimal? OriginLat = null,
    decimal? OriginLon = null
);
