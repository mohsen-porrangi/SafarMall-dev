using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Query.Transactions.GetTransactionStatus
{
    public class GetTransactionStatusHandler(IUnitOfWork unitOfWork)
        : IQueryHandler<GetTransactionStatusQuery, TransactionStatusDto?>
    {
        public async Task<TransactionStatusDto?> Handle(GetTransactionStatusQuery request, CancellationToken cancellationToken)
        {
            var transaction = await unitOfWork.Transactions.GetByIdAsync(
                request.TransactionId, cancellationToken: cancellationToken);

            if (transaction == null)
                return null;

            return new TransactionStatusDto
            {
                TransactionId = transaction.Id,
                Status = transaction.Status,
                Amount = transaction.Amount.Value,
                Currency = transaction.Amount.Currency,
                TransactionDate = transaction.TransactionDate,
                ProcessedAt = transaction.ProcessedAt
            };
        }
    }
}
