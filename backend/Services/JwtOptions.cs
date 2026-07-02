namespace VirtualTryOn.Api.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "VirtualTryOn.Api";

    public string Audience { get; set; } = "VirtualTryOn.Frontend";

    public string Secret { get; set; } = "CHANGE_ME_FOR_PRODUCTION_Use_Environment_Variables_At_Least_32_Bytes";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;

    public int RememberMeRefreshTokenDays { get; set; } = 30;
}
