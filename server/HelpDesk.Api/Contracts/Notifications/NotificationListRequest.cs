using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Notifications;

/// <summary>Pagination and unread filtering for the authenticated user's notifications.</summary>
public sealed class NotificationListRequest
{
    [Range(1, int.MaxValue)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public bool UnreadOnly { get; init; }
}
