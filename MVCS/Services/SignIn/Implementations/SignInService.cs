using CineStackAPI.MVCS.Models.Authorization;
using CineStackAPI.MVCS.Models.SignIn;
using CineStackAPI.MVCS.Services.Authentication.Interfaces;
using CineStackAPI.MVCS.Services.SignIn.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace CineStackAPI.MVCS.Services.SignIn.Implementations;

public sealed class SignInService(
    IJwtTokenService jwtTokenService,
    UserManager<ApplicationUserModel> userManager,
    SignInManager<ApplicationUserModel> signInManager) : ISignInService
{
    /// <summary>
    /// SIGNS IN A USER WITH THE PROVIDED CREDENTIALS.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<SignInResponseModel> SignInAsync(SignInRequestModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(model.Password) == true ||
            (string.IsNullOrWhiteSpace(model.Email) == true && string.IsNullOrWhiteSpace(model.UserName) == true))
        {
            return InvalidCredentials();
        }

        ApplicationUserModel? user = string.IsNullOrWhiteSpace(model.Email) == false
            ? await userManager.FindByEmailAsync(model.Email)
            : await userManager.FindByNameAsync(model.UserName!);

        if (user is null)
            return InvalidCredentials();

        // This API has no second-factor challenge flow. Never issue a password-only
        // JWT for an account requiring two-factor authentication.
        if (await userManager.GetTwoFactorEnabledAsync(user))
            return InvalidCredentials();

        SignInResult result;
        try
        {
            result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        }
        catch (FormatException)
        {
            // Legacy malformed password hashes cannot authenticate.
            return InvalidCredentials();
        }

        if (!result.Succeeded)
            return InvalidCredentials();

        JwtTokenResponse token = await jwtTokenService.GenerateJwtTokenAsync(new JwtTokenModel
        {
            Subject = user.Id,
            UserName = user.UserName ?? user.Email,
            Email = user.Email,
            UserType = user.UserType
        });

        return new SignInResponseModel
        {
            IsSuccess = token.IsSuccess,
            Token = token.Token,
            Message = token.IsSuccess == true ? token.Message : ExternalMessages.InternalServerError
        };
    }

    private static SignInResponseModel InvalidCredentials() => new()
    {
        IsSuccess = false,
        AuthenticationFailed = true,
        Message = "Unable to sign in with these credentials."
    };
}
