namespace Train.API.Models.Responses;
public class BaseResponseDTO<T>
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }
}
