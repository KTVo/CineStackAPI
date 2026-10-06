using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Models.Authorization;

public sealed class JwtTokenResponse : BaseResponseModel
{
    public string? Token { get; set; }
}
