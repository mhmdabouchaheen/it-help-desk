namespace HelpDesk.Api.Hubs;

/// <summary>Stable server-to-client method names exposed by the notification hub.</summary>
public static class NotificationHubEvents
{
    /// <summary>Signals that authoritative notification REST state should be reloaded.</summary>
    public const string NotificationCreated = "NotificationCreated";
}
