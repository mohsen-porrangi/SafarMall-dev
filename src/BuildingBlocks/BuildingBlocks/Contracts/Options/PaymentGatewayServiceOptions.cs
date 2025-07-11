namespace BuildingBlocks.Contracts.Options
{
    /// <summary>
    /// Configuration options for Paymentr Service external API integration
    /// </summary>
    public sealed record PaymentGatewayServiceOptions : IExternalServiceOptions
    {
        public const string SectionName = "ExternalServices:PaymentGateway";
        

        public required string BaseUrl { get; init; }
        public required PaymentEndpointsConfig Endpoints { get; init; }
        public int Timeout { get; init; } = 30;
        
    }
    /// <summary>
    /// Payment Service endpoint configuration
    /// </summary>
    public sealed record PaymentEndpointsConfig
    {
        public required string CreatePayment { get; init; }
        public required string VerifyPayment { get; init; }
        public required string GetPaymentStatus { get; init; }
    }
}
