namespace VirtualTryOn.Api.Responses;

public sealed record ApiMessage(string Message, string? ResetLink = null);
