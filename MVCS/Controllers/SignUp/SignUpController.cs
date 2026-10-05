using BaseEFAPI.Helpers.Messages;
using BaseEFAPI.MVCS.Services.Registration.Interfaces;
using BaseEFAPI.Statics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseEFAPI.MVCS.Controllers.SignUp;

[ApiController]
[Route("api/v1/signup")]
[AllowAnonymous]
public class SignUpController(IRegistrationService registrationService, ILogger<SignUpController> logger) : ControllerBase
{
    private readonly IRegistrationService _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
    private readonly ILogger<SignUpController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// REGISTERS A GENERAL USER WITH THE PROVIDED CREDENTIALS
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("register/general")]
    public async Task<IActionResult> SignUpGeneral([FromBody] SignUpRequestModel request)
    {
        // NULL CHECKS
        if (request == null) { return BadRequest(ExternalMessages.RequestBodyIsNull); }

        if (string.IsNullOrEmpty(request.Username) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.UserNameIsNull));
            return BadRequest(ExternalMessages.UserNameIsNull);
        }
        if (string.IsNullOrEmpty(request.Email) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.EmailIsNull));
            return BadRequest(ExternalMessages.EmailIsNull);
        }
        if (string.IsNullOrEmpty(request.Password) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.PasswordIsNull));
            return BadRequest(ExternalMessages.PasswordIsNull);
        }

        SignUpResponseModel response = await _registrationService.RegisterUserAsync(new ApplicationUserModel
        {
            UserName = request.Username,
            Email = request.Email,
            UserType = ProgramConstants.UserTypeUser,
            CreatedAt = DateTime.UtcNow,
        }, request.Password);

        if (response.IsSuccess == false)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// REGISTERS AN ADMIN USER WITH THE PROVIDED CREDENTIALS
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <summary>
    /// REGISTERS AN ADMIN USER WITH THE PROVIDED CREDENTIALS
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("register/admin")]
    public async Task<IActionResult> SignUpAdmin([FromBody] SignUpRequestModel request)
    {
        // NULL CHECKS
        if (request == null) { return BadRequest(ExternalMessages.RequestBodyIsNull); }

        if (string.IsNullOrEmpty(request.Username) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.UserNameIsNull));
            return BadRequest(ExternalMessages.UserNameIsNull);
        }
        if (string.IsNullOrEmpty(request.Email) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.EmailIsNull));
            return BadRequest(ExternalMessages.EmailIsNull);
        }
        if (string.IsNullOrEmpty(request.Password) == true)
        {
            _logger.LogWarning(MessageGenerator.RequestErrorMessage(className: "SignUpController", methodName: "SignUp", requestBody: request, extraInfo: ExternalMessages.PasswordIsNull));
            return BadRequest(ExternalMessages.PasswordIsNull);
        }

        SignUpResponseModel response = await _registrationService.RegisterUserAsync(new ApplicationUserModel
        {
            UserName = request.Username,
            Email = request.Email,
            UserType = ProgramConstants.UserTypeAdmin,
            CreatedAt = DateTime.UtcNow,
        }, request.Password);

        if (response.IsSuccess == false)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
