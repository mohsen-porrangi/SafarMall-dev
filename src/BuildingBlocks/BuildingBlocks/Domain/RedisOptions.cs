namespace Simple.Application.Model.OptionPatternModels;
public class RedisOptions
{
    public const string Name = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "App";
}
