namespace BaseEFAPI.MVCS.Services.Registration.Interfaces;

public interface IRegistrationService
{
    Task<SignUpResponseModel> RegisterUserAsync(ApplicationUserModel user, string password);
}
