using CineStackAPI.MVCS.Models._Base;

namespace CineStackAPI.MVCS.Models.CRUD;
public sealed class CRUDResponse : BaseResponseModel
{
    public string? DataAsString { get; set; }
}
