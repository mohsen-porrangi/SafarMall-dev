using System;
using WalletApp.Domain.Enums;

namespace WalletApp.Application.Features.Query.Transactions.GetTransactionStatus
{
    public record GetTransactionStatusQuery(Guid TransactionId) : IQuery<TransactionStatusDto>;

    public record TransactionStatusDto
    {
        public Guid TransactionId { get; init; }
        public TransactionStatus Status { get; init; }
        public decimal Amount { get; init; }
        public CurrencyCode Currency { get; init; }
        public DateTime TransactionDate { get; init; }
        public DateTime? ProcessedAt { get; init; }
    };
}
