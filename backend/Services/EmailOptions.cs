namespace VirtualTryOn.Api.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public bool EnableSsl { get; set; } = true;
}
