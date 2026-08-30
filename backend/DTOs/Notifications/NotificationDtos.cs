namespace VirtualTryOn.Api.DTOs.Notifications;

using System.ComponentModel.DataAnnotations;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationResponse> Items,
    int UnreadCount);

public sealed class BroadcastNotificationRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 2)]
    public string Message { get; set; } = string.Empty;

    [StringLength(30)]
    public string Type { get; set; } = "announcement";

    [StringLength(2048)]
    public string? Link { get; set; }

    [RegularExpression("^(All|Customer|Admin)$",
        ErrorMessage = "Audience must be All, Customer, or Admin.")]
    public string Audience { get; set; } = "All";
}

public sealed record BroadcastNotificationResponse(
    int RecipientCount,
    DateTimeOffset SentAt);
