using SMS.API.Models.Requests;

namespace SMS.API.Models.Responses;
public class KavenegarResponseModel<T>
{
    public KavenegarPrimaryResponse Return { get; set; }
    public List<T>? Entries { get; set; }
}
