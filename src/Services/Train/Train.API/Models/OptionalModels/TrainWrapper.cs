namespace Train.API.Models.OptionalModels;

public class TrainWrapper
{
    public const string Name = "TrainWrapperSettings";
    public required string BaseUrl { get; set; }
    public required string BaseRoute { get; set; }
}
