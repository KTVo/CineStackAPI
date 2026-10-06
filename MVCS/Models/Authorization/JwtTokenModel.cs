namespace CineStackAPI.MVCS.Models.Authorization;

public sealed class JwtTokenModel
{
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public DateTime ExpireOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? EncryptedSecretKey { get; set; }
    public string? JTI { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? UserType { get; set; }
}
