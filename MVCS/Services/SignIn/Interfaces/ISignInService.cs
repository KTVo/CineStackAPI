using BaseEFAPI.MVCS.Models.SignIn;

namespace BaseEFAPI.MVCS.Services.SignIn.Interfaces;

public interface ISignInService
{
    Task<SignInResponseModel> SignInAsync(SignInRequestModel model);
}
