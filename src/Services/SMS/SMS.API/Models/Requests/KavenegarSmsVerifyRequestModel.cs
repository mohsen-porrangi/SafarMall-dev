namespace SMS.API.Models.Requests;
public class KavenegarSmsVerifyRequestModel
{
    public string Receptor { get; set; }
    public string Token { get; set; }
    public string Template { get; set; }
}
