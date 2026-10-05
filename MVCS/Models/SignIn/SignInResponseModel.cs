namespace BaseEFAPI.MVCS.Models.SignIn;

public sealed class SignInResponseModel : BaseResponseModel
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AuthenticationFailed { get; set; }

    public ApplicationUserModel? User { get; set; }
    public string? Token { get; set; }
}
