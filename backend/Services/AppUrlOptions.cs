namespace VirtualTryOn.Api.Services;

public sealed class AppUrlOptions
{
    public const string SectionName = "AppUrls";

    public string FrontendBaseUrl { get; set; } = "http://192.168.10.155:5173";
}
