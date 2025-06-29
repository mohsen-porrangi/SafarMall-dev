namespace Train.API.Models.Responses;

public class CaptchaResponseDTO
{
    public int Id { get; set; }
    public byte[] Image { get; set; }
}
