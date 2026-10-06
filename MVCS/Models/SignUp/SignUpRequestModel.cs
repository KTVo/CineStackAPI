using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Models.SignUp;

public sealed class SignUpRequestModel : BaseRequestModel
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    // Plaintext input; RegistrationService hashes it before storing it.
    public string? Password { get; set; }
}
