namespace CineStackAPI.MVCS.Models._Base;

public class BaseResponseModel
{
    public bool? IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

}