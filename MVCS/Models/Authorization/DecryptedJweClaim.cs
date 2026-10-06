namespace CineStackAPI.MVCS.Models.Authorization;

public sealed class DecryptedJweClaim
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}
