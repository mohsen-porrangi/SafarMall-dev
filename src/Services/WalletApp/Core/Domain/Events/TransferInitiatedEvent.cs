using BuildingBlocks.Enums;
using BuildingBlocks.MessagingEvent.Base;
using BuildingBlocks.ValueObjects;

namespace WalletApp.Domain.Events;

/// <summary>
/// Transfer initiated event
/// </summary>
public record TransferInitiatedEvent : IntegrationEvent
{
    public TransferInitiatedEvent(
        Guid fromTransactionId,
        Guid fromWalletId,
        Money amount,
        string transferReference)
    {
        FromTransactionId = fromTransactionId;
        FromWalletId = fromWalletId;
        Amount = amount.Value;
        Currency = amount.Currency;
        TransferReference = transferReference;
        InitiatedAt = DateTime.UtcNow;
        Source = "Wallet";
    }

    public Guid FromTransactionId { get; }
    public Guid FromWalletId { get; }
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }
    public string TransferReference { get; }
    public DateTime InitiatedAt { get; }
}

/// <summary>
/// Transfer completed event
/// </summary>
public record TransferCompletedEvent : IntegrationEvent
{
    public TransferCompletedEvent(
        Guid fromTransactionId,
        Guid toTransactionId,
        Guid toWalletId,
        Money amount,
        decimal newBalance)
    {
        FromTransactionId = fromTransactionId;
        ToTransactionId = toTransactionId;
        ToWalletId = toWalletId;
        Amount = amount.Value;
        Currency = amount.Currency;
        NewBalance = newBalance;
        CompletedAt = DateTime.UtcNow;
        Source = "Wallet";
    }

    public Guid FromTransactionId { get; }
    public Guid ToTransactionId { get; }
    public Guid ToWalletId { get; }
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }
    public decimal NewBalance { get; }
    public DateTime CompletedAt { get; }
}