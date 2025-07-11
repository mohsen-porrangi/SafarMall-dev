using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletApp.Domain.Enums;

namespace WalletApp.Application.Features.Query.Transactions.GetTransactionDetails
{
    public record GetTransactionDetailsQuery(Guid TransactionId) : IQuery<TransactionDetailsDto>;

    public record TransactionDetailsDto
    {
        public Guid Id { get; init; }
        public string TransactionNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public CurrencyCode Currency { get; init; }
        public TransactionDirection Direction { get; init; }
        public TransactionType Type { get; init; }
        public TransactionStatus Status { get; init; }
        public string Description { get; init; } = string.Empty;
        public string? OrderContext { get; init; }
        public string? PaymentReferenceId { get; init; }
        public DateTime TransactionDate { get; init; }
        public DateTime? ProcessedAt { get; init; }
        public bool IsRefundable { get; init; }
        public Guid UserId { get; init; }
        public Guid WalletId { get; init; }
    };
}
