namespace WalletApp.Application.Features.Query.Wallets.GetWalletStatus
{
    public record GetWalletStatusQuery(Guid UserId) : IQuery<WalletStatusDto>;

    public record WalletStatusDto
    {
        public bool HasWallet { get; init; }
        public bool IsActive { get; init; }
        public decimal TotalBalanceInIrr { get; init; }
        public bool CanMakePayment { get; init; }
    };
}
