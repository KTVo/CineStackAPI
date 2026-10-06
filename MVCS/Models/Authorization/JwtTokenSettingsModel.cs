namespace CineStackAPI.MVCS.Models.Authorization;

public sealed class JwtTokenSettingsModel
{
    public string? SecretKey { get; set; }
    public string? EncryptionKey { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpiryInMinutes { get; set; } = 60;
}
