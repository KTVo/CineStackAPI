using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using CineStackAPI.MVCS.Models.Authorization;
using CineStackAPI.MVCS.Services.Authentication.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

public sealed class JwtRoleTests
{
    [Theory]
    [InlineData("ADMIN", true)]
    [InlineData("USER", false)]
    public async Task Validated_token_recognizes_only_the_assigned_role(string role, bool isAdmin)
    {
        string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        string encryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtSettings:SecretKey"] = signingKey,
            ["Authentication:JwtSettings:EncryptionKey"] = encryptionKey,
            ["Authentication:JwtSettings:Issuer"] = "test-issuer",
            ["Authentication:JwtSettings:Audience"] = "test-audience"
        }).Build();
        var service = new JwtTokenService(configuration);
        var response = await service.GenerateJwtTokenAsync(new JwtTokenModel
        {
            Subject = "user-id", UserName = "alice", Email = "alice@example.com", UserType = role
        });
        Assert.True(response.IsSuccess);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(response.Token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)),
            ValidateIssuer = true, ValidIssuer = "test-issuer",
            ValidateAudience = true, ValidAudience = "test-audience",
            ValidateLifetime = true,
            RoleClaimType = "role"
        }, out _);

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.True(principal.IsInRole(role));
        Assert.Equal(isAdmin, principal.IsInRole("ADMIN"));
        Assert.True(service.DecryptJWEToken(response.Token!).IsSuccess);
    }
}
