using BuildingBlocks.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;
using static WalletApp.Application.Common.Interfaces.ITransferFeeCalculator;

namespace WalletApp.Infrastructure.Services;

/// <summary>
/// Transfer fee calculator implementation
/// SOLID: Single responsibility - only calculates transfer fees
/// KISS: Simple percentage-based calculation
/// </summary>
public class TransferFeeCalculator : ITransferFeeCalculator
{
    private readonly ILogger<TransferFeeCalculator> _logger;
    private readonly TransferFeeConfiguration _config;

    public TransferFeeCalculator(
        IConfiguration configuration,
        ILogger<TransferFeeCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = new TransferFeeConfiguration();
        configuration.GetSection("TransferFee").Bind(_config);
    }

    /// <summary>
    /// Calculate transfer fee based on amount and business rules
    /// </summary>
    public Money CalculateFee(Money transferAmount, bool isInternalTransfer = true)
    {
        if (transferAmount == null)
            throw new ArgumentNullException(nameof(transferAmount));

        if (transferAmount.Value <= 0)
            throw new ArgumentException("Transfer amount must be positive", nameof(transferAmount));

        try
        {
            // Business Rules for fee calculation
            var feeAmount = isInternalTransfer
                ? CalculateInternalTransferFee(transferAmount)
                : CalculateExternalTransferFee(transferAmount);

            _logger.LogDebug(
                "Transfer fee calculated: Amount: {Amount} {Currency}, Fee: {Fee} {Currency}, Internal: {IsInternal}",
                transferAmount.Value, transferAmount.Currency, feeAmount, transferAmount.Currency, isInternalTransfer);

            return Money.Create(feeAmount, transferAmount.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transfer fee for amount: {Amount}", transferAmount.Value);
            throw;
        }
    }

    /// <summary>
    /// Check if transfer amount is eligible for fee waiver
    /// </summary>
    public bool IsEligibleForFeeWaiver(Money transferAmount, int userTransferCountThisMonth = 0)
    {
        if (transferAmount == null)
            return false;

        // Business Rule 1: Free transfers under minimum amount
        if (transferAmount.Value <= _config.FreeTransferThreshold)
        {
            _logger.LogDebug("Fee waived - amount below threshold: {Amount}", transferAmount.Value);
            return true;
        }

        // Business Rule 2: First N transfers per month are free
        if (userTransferCountThisMonth < _config.FreeTransfersPerMonth)
        {
            _logger.LogDebug("Fee waived - free monthly transfer: {Count}/{Limit}",
                userTransferCountThisMonth + 1, _config.FreeTransfersPerMonth);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get fee breakdown for transparency
    /// </summary>
    public TransferFeeBreakdown GetFeeBreakdown(Money transferAmount, bool isInternalTransfer = true)
    {
        var baseFee = CalculateFee(transferAmount, isInternalTransfer);
        var isWaived = IsEligibleForFeeWaiver(transferAmount);

        return new TransferFeeBreakdown
        {
            TransferAmount = transferAmount,
            BaseFee = baseFee,
            ActualFee = isWaived ? Money.Zero(transferAmount.Currency) : baseFee,
            IsWaived = isWaived,
            FeePercentage = isInternalTransfer ? _config.InternalFeePercentage : _config.ExternalFeePercentage
        };
    }

    #region Private Methods

    /// <summary>
    /// Calculate fee for internal transfers (within same wallet system)
    /// </summary>
    private decimal CalculateInternalTransferFee(Money transferAmount)
    {
        // Internal transfers have lower fees
        var feeAmount = transferAmount.Value * (_config.InternalFeePercentage / 100);

        // Apply minimum and maximum fee limits
        feeAmount = Math.Max(feeAmount, _config.MinimumFee);
        feeAmount = Math.Min(feeAmount, _config.MaximumFee);

        return feeAmount;
    }

    /// <summary>
    /// Calculate fee for external transfers (to bank accounts)
    /// </summary>
    private decimal CalculateExternalTransferFee(Money transferAmount)
    {
        // External transfers have higher fees
        var feeAmount = transferAmount.Value * (_config.ExternalFeePercentage / 100);

        // Apply minimum and maximum fee limits
        feeAmount = Math.Max(feeAmount, _config.MinimumFee);
        feeAmount = Math.Min(feeAmount, _config.MaximumFee);

        return feeAmount;
    }

    #endregion
}

/// <summary>
/// Transfer fee configuration
/// </summary>
public class TransferFeeConfiguration
{
    /// <summary>
    /// Fee percentage for internal transfers (default: 0.1%)
    /// </summary>
    public decimal InternalFeePercentage { get; set; } = 0.1m;

    /// <summary>
    /// Fee percentage for external transfers (default: 0.5%)
    /// </summary>
    public decimal ExternalFeePercentage { get; set; } = 0.5m;

    /// <summary>
    /// Minimum fee amount (default: 1000 IRR)
    /// </summary>
    public decimal MinimumFee { get; set; } = 1000;

    /// <summary>
    /// Maximum fee amount (default: 50000 IRR)
    /// </summary>
    public decimal MaximumFee { get; set; } = 50000;

    /// <summary>
    /// Amount below which transfers are free (default: 10000 IRR)
    /// </summary>
    public decimal FreeTransferThreshold { get; set; } = 10000;

    /// <summary>
    /// Number of free transfers per month (default: 3)
    /// </summary>
    public int FreeTransfersPerMonth { get; set; } = 3;
}