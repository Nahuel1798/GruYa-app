namespace GruYaApi.Service;

public interface INotificationService
{
    // Assistance lifecycle
    Task NotifyDirectedAssistanceToProviderAsync(int providerUserId, int assistanceId, string serviceType, string issueType);
    Task NotifyNewAssistanceToProvidersAsync(Dictionary<int, string> recipientTokens, int assistanceId, string serviceType, string issueType, decimal originLat, decimal originLon);
    Task NotifyTripStartedToClientAsync(int clientUserId, int assistanceId, int providerId, string trackingSessionId);
    Task NotifyProviderArrivedToClientAsync(int clientUserId, int assistanceId, int providerId);
    Task NotifyProviderHeadingToDestinationToClientAsync(int clientUserId, int assistanceId, int providerId);
    Task NotifyServiceCompletedToClientAsync(int clientUserId, int assistanceId, int providerId);

    // Quote lifecycle
    Task NotifyNewQuoteToClientAsync(int clientUserId, int assistanceId, int quoteId, string providerName, decimal price);
    Task NotifyQuoteAcceptedToProviderAsync(int providerUserId, int assistanceId, int providerProfileId);
    Task NotifyQuoteAcceptedToClientAsync(int clientUserId, int assistanceId, string companyName);
    Task NotifyQuoteRejectedToProviderAsync(int providerUserId, int assistanceId);
}
