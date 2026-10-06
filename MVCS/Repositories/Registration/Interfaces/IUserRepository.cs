using CineStackAPI.MVCS.Repositories.Responses;

public interface IUserRepository
{
    Task<SignUpResponseModel> AddUserAsync(ApplicationUserModel user);
    Task<ApplicationUserResponse> GetUserByEmailAsync(string email);
    Task<ApplicationUserResponse> GetUserByUsernameAsync(string username);
}