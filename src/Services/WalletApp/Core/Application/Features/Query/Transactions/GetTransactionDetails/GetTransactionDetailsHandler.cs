using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Query.Transactions.GetTransactionDetails
{
    public class GetTransactionDetailsHandler(IUnitOfWork unitOfWork)
        : IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto?>
    {
        public async Task<TransactionDetailsDto?> Handle(GetTransactionDetailsQuery request, CancellationToken cancellationToken)
        {
            var transaction = await unitOfWork.Transactions.GetByIdAsync(
                request.TransactionId, cancellationToken: cancellationToken);

            if (transaction == null)
                return null;

            return new TransactionDetailsDto
            {
                Id = transaction.Id,
                TransactionNumber = transaction.TransactionNumber.Value,
                Amount = transaction.Amount.Value,
                Currency = transaction.Amount.Currency,
                Direction = transaction.Direction,
                Type = transaction.Type,
                Status = transaction.Status,
                Description = transaction.Description,
                OrderContext = transaction.OrderContext,
                PaymentReferenceId = transaction.PaymentReferenceId,
                TransactionDate = transaction.TransactionDate,
                ProcessedAt = transaction.ProcessedAt,
                IsRefundable = transaction.IsRefundable(),
                UserId = transaction.UserId,
                WalletId = transaction.WalletId
            };
        }
    }
}
