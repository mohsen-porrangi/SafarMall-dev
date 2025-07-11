namespace BuildingBlocks.Domain;
public class MongoOptions
{
    public const string Name = "MongoDB";
    public string ConnectionString { get; set; }
    public string LogDatabaseName { get; set; }
}

