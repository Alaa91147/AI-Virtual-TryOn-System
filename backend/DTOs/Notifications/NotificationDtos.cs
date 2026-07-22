namespace VirtualTryOn.Api.DTOs.Notifications;

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