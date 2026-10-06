using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Repositories.Responses;
public class ApplicationUserResponse : BaseResponseModel
{
    public ApplicationUserModel? User { get; set; }
}