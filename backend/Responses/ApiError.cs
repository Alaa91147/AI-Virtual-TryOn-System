namespace VirtualTryOn.Api.Responses;

public sealed record ApiError(string Message, IReadOnlyCollection<string> Errors)
{
    public static ApiError Create(string message, params string[] errors)
    {
        return new ApiError(message, errors);
    }
}
