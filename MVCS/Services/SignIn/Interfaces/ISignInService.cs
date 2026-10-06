using CineStackAPI.MVCS.Models.SignIn;

namespace CineStackAPI.MVCS.Services.SignIn.Interfaces;

public interface ISignInService
{
    Task<SignInResponseModel> SignInAsync(SignInRequestModel model);
}
