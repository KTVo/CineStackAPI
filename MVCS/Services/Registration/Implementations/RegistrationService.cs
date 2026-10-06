using CineStackAPI.MVCS.Services.Registration.Interfaces;
using Microsoft.AspNetCore.Identity;

public sealed class RegistrationService(UserManager<ApplicationUserModel> userManager) : IRegistrationService
{
    public async Task<SignUpResponseModel> RegisterUserAsync(ApplicationUserModel user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Identity validates the password and user, hashes the password, normalizes
        // identifiers, initializes stamps/lockout, and persists through its EF store.
        IdentityResult result = await userManager.CreateAsync(user, password);
        return new SignUpResponseModel
        {
            IsSuccess = result.Succeeded,
            Message = result.Succeeded ? ExternalMessages.SignUpSuccess : ExternalMessages.SignUpFailure,
            Errors = result.Succeeded ? null : result.Errors.Select(error => error.Description).ToList()
        };
    }
}
