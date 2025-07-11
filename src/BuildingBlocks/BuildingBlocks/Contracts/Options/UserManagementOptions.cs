namespace BuildingBlocks.Contracts.Options
{
    public class UserManagementOptions : IExternalServiceOptions
    {
        public const string SectionName = "ExternalServices:UserManagement";
        public required string BaseUrl { get; set; }
        public required EndpointsConfig Endpoints { get; set; }
        public int Timeout { get; init; } = 30;

        public class EndpointsConfig
        {
            public required string GetUserIds { get; set; }
        }
    }
}
