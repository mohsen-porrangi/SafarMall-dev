namespace Train.API.Models.Requests;
public class SearchPassengersRequestDTO
{
    public int Adult { get; set; }
    public int Child { get; set; }
    public int Infant { get; set; }
}
