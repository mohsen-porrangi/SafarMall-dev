namespace BuildingBlocks.Contracts.Options
{
    /// <summary>
    /// Configuration options for Order Service external API integration
    /// </summary>
    public sealed record OrderServiceOptions : IExternalServiceOptions
    {
        public const string SectionName = "ExternalServices:OrderService";

        public required string BaseUrl { get; init; }
        public required OrderEndpointsConfig Endpoints { get; init; }
        public int Timeout { get; init; } = 30;
    }

    /// <summary>
    /// Order Service endpoint configuration
    /// </summary>
    public sealed record OrderEndpointsConfig
    {
        public required string CreateTrainOrder { get; init; }
        public required string GetOrderDetails { get; init; }
        public required string UpdateOrderStatus { get; init; }
        public required string CompeleteOrder { get; set; }
        public required string CancelOrder { get; init; }
    }
}
