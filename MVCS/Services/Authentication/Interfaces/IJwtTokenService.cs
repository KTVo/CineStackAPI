using CineStackAPI.MVCS.Models.Authorization;

namespace CineStackAPI.MVCS.Services.Authentication.Interfaces;

public interface IJwtTokenService
{
    Task<JwtTokenResponse> GenerateJwtTokenAsync(JwtTokenModel jwtToken);
    DecryptedJweTokenResponse DecryptJWEToken(string tokenString);
}
