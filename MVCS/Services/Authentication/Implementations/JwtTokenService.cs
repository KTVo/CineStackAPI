using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CineStackAPI.MVCS.Models.Authorization;
using CineStackAPI.MVCS.Services.Authentication.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace CineStackAPI.MVCS.Services.Authentication.Implementations;

public sealed class JwtTokenService : IJwtTokenService
{
    // PRIVATE CLASS VARIABLES
    private readonly IConfiguration _configuration;
    private readonly JwtTokenSettingsModel _jwtSettings;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _jwtSettings = GetJwtSettings();
    }
    private JwtTokenSettingsModel GetJwtSettings()
    {
        JwtTokenSettingsModel model = new()
        {
            SecretKey = _configuration["Authentication:JwtSettings:SecretKey"],
            Issuer = _configuration["Authentication:JwtSettings:Issuer"],
            Audience = _configuration["Authentication:JwtSettings:Audience"],
            ExpiryInMinutes = int.Parse(_configuration["Authentication:JwtSettings:ExpiryInMinutes"] ?? "60"),
            EncryptionKey = _configuration["Authentication:JwtSettings:EncryptionKey"]
        };

        if (string.IsNullOrEmpty(model.SecretKey) == true) { throw new ArgumentNullException(ExternalMessages.SecretKeyIsNull); }
        if (string.IsNullOrEmpty(model.Issuer) == true) { throw new ArgumentNullException(ExternalMessages.IssuerIsNull); }
        if (string.IsNullOrEmpty(model.Audience) == true) { throw new ArgumentNullException(ExternalMessages.AudienceIsNull); }
        if (string.IsNullOrEmpty(model.EncryptionKey) == true) { throw new ArgumentNullException(ExternalMessages.EncryptionKeyIsNull); }

        return model;
    }

    public async Task<JwtTokenResponse> GenerateJwtTokenAsync(JwtTokenModel jwtToken)
    {
        string? userId = jwtToken.Subject;
        string? userName = jwtToken.UserName;
        string? userEmail = jwtToken.Email;
        string? userType = jwtToken.UserType;

        // NULL CHECKS
        if (string.IsNullOrEmpty(userId) == true) { return new() { IsSuccess = false, Message = ExternalMessages.UserNameIsNull }; }
        if (string.IsNullOrEmpty(userName) == true) { return new() { IsSuccess = false, Message = ExternalMessages.UserNameIsNull }; }
        if (string.IsNullOrEmpty(userEmail) == true) { return new() { IsSuccess = false, Message = ExternalMessages.EmailIsNull }; }
        if (string.IsNullOrEmpty(userType) == true) { return new() { IsSuccess = false, Message = ExternalMessages.UserTypeIsNull }; }

        try
        {
            // CREATE CLAIMS FOR JWT TOKEN
            List<Claim> claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, userEmail),
                new("role", userType),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // CREATE SYMMETRIC SECURITY KEY FOR JWT TOKEN
#pragma warning disable CS8604 // Possible null reference argument.
            SymmetricSecurityKey key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
#pragma warning restore CS8604 // Possible null reference argument.

            // CREATE SIGNING CREDENTIALS FOR JWT TOKEN
            SigningCredentials signedCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            // BASE64 -> 32 BYTES
#pragma warning disable CS8604 // Possible null reference argument.
            byte[] encryptionKeyBytes =
                Convert.FromBase64String(_jwtSettings.EncryptionKey);
#pragma warning restore CS8604 // Possible null reference argument.

            // CREATE ENCRYPTION KEY
            SymmetricSecurityKey encryptionKey =
                new SymmetricSecurityKey(encryptionKeyBytes);

            // CREATE ENCRYPTION CREDENTIALS
            EncryptingCredentials encryptingCredentials =
                new EncryptingCredentials(
                    encryptionKey,
                    SecurityAlgorithms.Aes256KW,
                    SecurityAlgorithms.Aes256CbcHmacSha512
                );

            JwtSecurityTokenHandler tokenHandler =
                new JwtSecurityTokenHandler();

            DateTime now = DateTime.UtcNow;

            JwtSecurityToken token =
                tokenHandler.CreateJwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    subject: new ClaimsIdentity(claims),
                    notBefore: now,
                    expires: now.AddMinutes(_jwtSettings.ExpiryInMinutes),
                    issuedAt: now,

                    // SIGN
                    signingCredentials: signedCredentials,

                    // ENCRYPT
                    encryptingCredentials: encryptingCredentials
                );

            string tokenString = tokenHandler.WriteToken(token);

            return new JwtTokenResponse { Token = tokenString, IsSuccess = true, Message = "JWT token generated successfully." };
        }
        catch (Exception ex)
        {
            return new() { IsSuccess = false, Message = ex.Message };
        }

    }

    /// <summary>
    /// DECRYPTS A JWE TOKEN AND RETURNS SERIALIZABLE CLAIM DATA.
    /// </summary>
    /// <param name="tokenString"></param>
    /// <returns></returns>
    public DecryptedJweTokenResponse DecryptJWEToken(string tokenString)
    {
        if (string.IsNullOrEmpty(tokenString) == true) { return new DecryptedJweTokenResponse { IsSuccess = false, Message = "Token string is null or empty." }; }

        try
        {
            // SIGNING KEY
#pragma warning disable CS8604 // Possible null reference argument.
            SymmetricSecurityKey signingSecurityKey =
                new(
                    Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
                );
#pragma warning restore CS8604 // Possible null reference argument.


            // JWE DECRYPTION KEY
#pragma warning disable CS8604 // Possible null reference argument.
            SymmetricSecurityKey encryptionSecurityKey =
                new(
                    Convert.FromBase64String(_jwtSettings.EncryptionKey)
                );
#pragma warning restore CS8604 // Possible null reference argument.

            // VALIDATION / DECRYPTION SETTINGS
            TokenValidationParameters validationParameters =
                new()
                {
                    // Decrypt outer JWE
                    TokenDecryptionKey = encryptionSecurityKey,

                    // Validate inner signed JWT
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingSecurityKey,

                    // Validate issuer
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,

                    // Validate audience
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,

                    // Validate expiration
                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    RoleClaimType = "role"
                };


            JwtSecurityTokenHandler tokenHandler = new()
            {
                MapInboundClaims = false
            };


            ClaimsPrincipal principal =
                tokenHandler.ValidateToken(
                    tokenString,
                    validationParameters,
                    out SecurityToken validatedToken
                );


            return new DecryptedJweTokenResponse
            {
                Claims = principal.Claims.Select(claim => new DecryptedJweClaim
                {
                    Type = claim.Type,
                    Value = claim.Value,
                    ValueType = claim.ValueType,
                    Issuer = claim.Issuer
                }).ToList(),
                IsSuccess = true,
                Message = "Token decrypted successfully."
            };
        }
        catch (Exception ex)
        {
            return new DecryptedJweTokenResponse { IsSuccess = false, Message = ex.Message };
        }
        

    }
}
