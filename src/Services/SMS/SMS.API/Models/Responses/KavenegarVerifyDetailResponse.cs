namespace SMS.API.Models.Requests;

public class KavenegarVerifyDetailResponse
{
    public long Messageid { get; set; }
    public string Message { get; set; }
    public long Status { get; set; }
    public string Statustext { get; set; }
    public long Sender { get; set; }
    public string Receptor { get; set; }
    public long Date { get; set; }
    public long Cost { get; set; }
}
