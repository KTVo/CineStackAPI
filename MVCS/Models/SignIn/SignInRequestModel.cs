using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Models.SignIn;

public sealed class SignInRequestModel : BaseRequestModel
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public required string Password { get; set; }
}
