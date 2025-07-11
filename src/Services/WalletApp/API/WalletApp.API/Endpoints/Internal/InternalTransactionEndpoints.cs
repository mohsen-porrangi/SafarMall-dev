using WalletApp.Application.Features.Query.Transactions.GetTransactionStatus;
using WalletApp.Application.Features.Query.Transactions.GetTransactionDetails;

namespace WalletApp.API.Endpoints.Internal;

/// <summary>
/// Internal transaction endpoints for Order service
/// </summary>
public class InternalTransactionEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var internalGroup = app.MapGroup("/api/internal/transactions")
            .WithTags("Internal-Transactions");

        internalGroup.MapGet("/{transactionId:guid}/status", async (
            Guid transactionId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetTransactionStatusQuery(transactionId);
            var result = await sender.Send(query, ct);

            if (result == null)
            {
                return Results.NotFound(new
                {
                    success = false,
                    error = "تراکنش یافت نشد",
                    transactionId
                });
            }

            return Results.Ok(new
            {
                success = true,
                transactionId = result.TransactionId,
                status = result.Status.ToString(),
                amount = result.Amount,
                currency = result.Currency.ToString(),
                transactionDate = result.TransactionDate,
                processedAt = result.ProcessedAt
            });
        })
            .WithName("GetTransactionStatusInternal")
            .WithSummary("Get transaction status (Internal)")
            .WithDescription("Get transaction status for Order service");

        internalGroup.MapGet("/{transactionId:guid}/details", async (
            Guid transactionId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetTransactionDetailsQuery(transactionId);
            var result = await sender.Send(query, ct);

            if (result == null)
            {
                return Results.NotFound(new
                {
                    success = false,
                    error = "تراکنش یافت نشد",
                    transactionId
                });
            }

            return Results.Ok(new
            {
                success = true,
                id = result.Id,
                transactionNumber = result.TransactionNumber,
                amount = result.Amount,
                currency = result.Currency.ToString(),
                direction = result.Direction.ToString(),
                type = result.Type.ToString(),
                status = result.Status.ToString(),
                description = result.Description,
                orderContext = result.OrderContext,
                paymentReferenceId = result.PaymentReferenceId,
                transactionDate = result.TransactionDate,
                processedAt = result.ProcessedAt,
                isRefundable = result.IsRefundable,
                userId = result.UserId,
                walletId = result.WalletId
            });
        })
            .WithName("GetTransactionDetailsInternal")
            .WithSummary("Get transaction details (Internal)")
            .WithDescription("Get complete transaction details for Order service");
    }
}