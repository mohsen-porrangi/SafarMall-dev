using BuildingBlocks.CQRS;

namespace WalletApp.Application.Features.Query.Wallets.GetWalletSummary;

/// <summary>
/// Get wallet summary query
/// </summary>
public record GetWalletSummaryQuery : IQuery<WalletSummaryDto>
{
    public Guid UserId { get; init; }
}


