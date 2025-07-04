namespace SMS.API.Models.OptionModels;
public class KavenegarSmsOptions
{
    public const string Name = "KavenegarSmsPanel";
    public required string WebServiceTemplate { get; set; }
    public required string APIKey { get; set; }
    public required string SmsWebSericeBasicURL { get; set; }
    public required string VerifyServiceRoute { get; set; }
    public required string SendSmsServiceRoute { get; set; }
    public required string DefultMessage { get; set; }
    public required int VerificationCodeTTL { get; set; }
}
