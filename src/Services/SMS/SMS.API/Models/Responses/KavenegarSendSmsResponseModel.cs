
namespace SMS.API.Models.Requests;
public class KavenegarSendSmsResponseModel
{
    public long Messageid { get; set; }
    public string message { get; set; }
    public int Status { get; set; }
    public string Statustext { get; set; }
    public string Sender { get; set; }
    public string Receptor { get; set; }
    public long Date { get; set; }
    public int Cost { get; set; }
}
