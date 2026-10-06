using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Models.Authorization;
public sealed class DecryptedJweTokenResponse : BaseResponseModel
{
    public List<DecryptedJweClaim> Claims { get; set; } = new List<DecryptedJweClaim>();
}
