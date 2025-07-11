namespace WalletApp.Application.Features.Query.Wallets.CheckAffordability
{
    public record CheckAffordabilityQuery(
      Guid UserId,
      decimal Amount,
      CurrencyCode Currency = CurrencyCode.IRR) : IQuery<AffordabilityDto>;

    public record AffordabilityDto
    {
        public bool CanAfford { get; init; }
        public string Reason { get; init; } = string.Empty;
        public decimal AvailableBalance { get; init; }
        public decimal RequiredAmount { get; init; }
        public decimal Shortfall { get; init; }
    };
}
