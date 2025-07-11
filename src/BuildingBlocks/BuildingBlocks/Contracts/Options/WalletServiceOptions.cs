namespace BuildingBlocks.Contracts.Options
{
    /// <summary>
    /// Configuration options for Wallet Service external API integration
    /// </summary>
    public sealed record WalletServiceOptions : IExternalServiceOptions
    {
        public const string SectionName = "ExternalServices:WalletService";

        public required string BaseUrl { get; init; }
        public required WalletEndpointsConfig Endpoints { get; init; }
        public int Timeout { get; init; } = 30;
    }

    /// <summary>
    /// Wallet Service endpoint configuration
    /// </summary>
    public sealed record WalletEndpointsConfig
    {
        public required string IntegratedPurchase { get; init; }
        public required string CheckAffordability { get; init; }
        public required string GetWalletBalance { get; init; }
        public required string GetWalletStatus { get; init; }
        public required string PaymentCallback { get; init; }
    }
}