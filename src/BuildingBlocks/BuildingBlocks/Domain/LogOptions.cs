namespace BuildingBlocks.Domain;

public class LogOptions
{
    public const string Name = "LogService";
    public required string MongoInfoCollection { get; set; }
    public required string MongoErrorCollection { get; set; }
    public required string MongoRequestCollection { get; set; }
    public required string LogFilePath { get; set; }
    public required string InternalErrorMongo { get; set; }
}
