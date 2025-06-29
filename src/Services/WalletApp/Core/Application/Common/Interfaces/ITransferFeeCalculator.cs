using BuildingBlocks.ValueObjects;

namespace WalletApp.Application.Common.Interfaces;

/// <summary>
/// Interface for calculating transfer fees
/// SOLID: Interface segregation - focused on fee calculation only
/// </summary>
public interface ITransferFeeCalculator
{
    /// <summary>
    /// Calculate transfer fee based on amount and transfer type
    /// </summary>
    /// <param name="transferAmount">Amount to transfer</param>
    /// <param name="isInternalTransfer">True for wallet-to-wallet, false for wallet-to-bank</param>
    /// <returns>Fee amount in same currency</returns>
    Money CalculateFee(Money transferAmount, bool isInternalTransfer = true);

    /// <summary>
    /// Check if transfer is eligible for fee waiver
    /// </summary>
    /// <param name="transferAmount">Amount to transfer</param>
    /// <param name="userTransferCountThisMonth">Number of transfers user has made this month</param>
    /// <returns>True if fee should be waived</returns>
    bool IsEligibleForFeeWaiver(Money transferAmount, int userTransferCountThisMonth = 0);

    /// <summary>
    /// Get detailed fee breakdown for transparency
    /// </summary>
    /// <param name="transferAmount">Amount to transfer</param>
    /// <param name="isInternalTransfer">True for wallet-to-wallet, false for wallet-to-bank</param>
    /// <returns>Detailed fee breakdown</returns>
    TransferFeeBreakdown GetFeeBreakdown(Money transferAmount, bool isInternalTransfer = true);
    /// <summary>
    /// Transfer fee breakdown for transparency
    /// Application layer model for fee calculation results
    /// </summary>
    public record TransferFeeBreakdown
    {
        public Money TransferAmount { get; init; } = null!;
        public Money BaseFee { get; init; } = null!;
        public Money ActualFee { get; init; } = null!;
        public bool IsWaived { get; init; }
        public decimal FeePercentage { get; init; }
        public string Reason => IsWaived ? "مشمول معافیت کارمزد" : $"کارمزد {FeePercentage}%";
    }
}