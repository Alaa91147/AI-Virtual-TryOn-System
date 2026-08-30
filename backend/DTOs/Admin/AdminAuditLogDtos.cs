namespace VirtualTryOn.Api.DTOs.Admin;

public sealed record AdminAuditLogResponse(
    Guid Id,
    Guid AdminUserId,
    string AdministratorName,
    string AdministratorEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress,
    DateTimeOffset CreatedAt);
