namespace BuildingBlocks.Contracts.Options
{
    public class UserManagementOptions
    {
        public required string BaseUrl { get; set; }
        public required EndpointsConfig Endpoints { get; set; }

        public class EndpointsConfig
        {
            public required string GetUserIds { get; set; }
        }
    }
}
