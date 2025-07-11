using BuildingBlocks.CQRS;

namespace WalletApp.Application.Features.Command.Credit.SettleCredit;

/// <summary>
/// Command to settle B2B credit
/// </summary>
public record SettleCreditCommand : ICommand<SettleCreditResponse>
{
    /// <summary>
    /// Credit ID to settle
    /// </summary>
    public Guid CreditId { get; init; }

    /// <summary>
    /// Settlement transaction ID (if payment received)
    /// </summary>
    public Guid? SettlementTransactionId { get; init; }

    /// <summary>
    /// Settlement notes/reason
    /// </summary>
    public string? SettlementNotes { get; init; }

    /// <summary>
    /// Force settlement even if overdue
    /// </summary>
    public bool ForceSettle { get; init; } = false;
}

/// <summary>
/// Response for credit settlement
/// </summary>
public record SettleCreditResponse
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? SettledAt { get; init; }
    public decimal? SettledAmount { get; init; }
}