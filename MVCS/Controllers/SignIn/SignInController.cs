using CineStackAPI.Helpers.Messages;
using CineStackAPI.MVCS.Models.Authorization;
using CineStackAPI.MVCS.Models.SignIn;
using CineStackAPI.MVCS.Services.Authentication.Interfaces;
using CineStackAPI.MVCS.Services.SignIn.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineStackAPI.MVCS.Controllers.SignIn;

[ApiController]
[Route("api/v1/signin")]
[AllowAnonymous]
public class SignInController(IJwtTokenService jwtTokenService, ISignInService signInService, ILogger<SignInController> logger) : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    private readonly ISignInService _signInService = signInService ?? throw new ArgumentNullException(nameof(signInService));
    private readonly ILogger<SignInController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// SIGNS IN A USER WITH THE PROVIDED CREDENTIALS
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequestModel request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Password) ||
            (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.UserName)))
        {
            return BadRequest("Email or username and password are required.");
        }

        SignInResponseModel response = await _signInService.SignInAsync(request);
        if (response.AuthenticationFailed)
            return Unauthorized(response);
        if (response.IsSuccess != true)
            return StatusCode(StatusCodes.Status500InternalServerError, ExternalMessages.InternalServerError);

        return Ok(response);
    }

#if DEBUG
    /// <summary>
    /// DECRYPTS A JWE TOKEN
    /// </summary>
    /// <param name="tokenString"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("test/decrypt/jwe/token")]
    public async Task<IActionResult> DecryptJWEToken([FromBody] string tokenString)
    {
        if (string.IsNullOrEmpty(tokenString) == true)
        {
#pragma warning disable CA2254 // Template should be a static expression
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignInController", methodName: "DecryptJWEToken", requestBody: tokenString, extraInfo: ExternalMessages.TokenStringIsNull));
#pragma warning restore CA2254 // Template should be a static expression
            return BadRequest(ExternalMessages.TokenStringIsNull);
        }

        DecryptedJweTokenResponse response = _jwtTokenService.DecryptJWEToken(tokenString);

        if (response.IsSuccess == false)
        {

#pragma warning disable CS8604 // Possible null reference argument.
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignInController", methodName: "DecryptJWEToken", requestBody: tokenString, extraInfo: response.Message));
#pragma warning restore CS8604 // Possible null reference argument.
            return StatusCode(StatusCodes.Status500InternalServerError, response.Message);
        }

        return Ok(response);
    }
#endif


}
