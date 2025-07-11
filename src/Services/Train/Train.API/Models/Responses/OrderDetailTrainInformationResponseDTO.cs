namespace Train.API.Models.Responses;
public class OrderDetailTrainInformationResponseDTO
{
    public long ID { get; set; }
    public long TicketNumber { get; set; }
    public int TicketSeries { get; set; }
    public int Fk_SaleCenterCode { get; set; }
    public string Name { get; set; }
    public string Family { get; set; }
    public string NationalCode { get; set; }
    public string Telephone { get; set; }
    public int Status { get; set; }
    public int CircularPeriod { get; set; }
    public int TrainNumber { get; set; }
    public int CircularNumberSerial { get; set; }
    public int WagonNumber { get; set; }
    public int CompartmentNumber { get; set; }
    public int SeatNumber { get; set; }
    public DateTime MoveDate { get; set; }
    public DateTime MoveDateTrain { get; set; }
    public string DepartureTime { get; set; }
    public int CompartmentCapicity { get; set; }
    public bool IsCompartment { get; set; }
    public int Formula10 { get; set; }
    public int WagonType { get; set; }
    public string WagonTypeName { get; set; }
    public int RationCode { get; set; }
    public string RattionName { get; set; }
    public int StartStation { get; set; }
    public string StartStationName { get; set; }
    public int EndStation { get; set; }
    public string EndStationName { get; set; }
    public int Fk_Tariff { get; set; }
    public string TariffName { get; set; }
    public int AxleCode { get; set; }
    public int Owner { get; set; }
    public string CompanyName { get; set; }
    public string SaleCenterName { get; set; }
    public int Degree { get; set; }
    public string TimeOfArrival { get; set; }
    public DateTime Register { get; set; }
    public int ReduplicateTicketNumber { get; set; }
}
