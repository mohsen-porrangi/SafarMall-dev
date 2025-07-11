using WalletApp.Application.Features.Command.Transactions.RefundTransaction;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.API.Endpoints.Transactions;

/// <summary>
/// Refund endpoint
/// </summary>
public class RefundEndpoint : ICarterModule
{
    [Authorize]
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Create refund        
        app.MapPost("/api/transactions/refund", async (
            RefundTransactionCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("CreateRefund")
            .WithTags("Transactions")
            .WithSummary("Create transaction refund")
            .WithDescription("Create a refund for a completed transaction")
            .RequireAuthorization();            

        // Get refundable transactions
        app.MapGet("/api/transactions/refundable", GetRefundableTransactionsAsync)
            .WithName("GetRefundableTransactions")
            .WithTags("Transactions")
            .WithSummary("Get refundable transactions")
            .WithDescription("Get list of transactions that can be refunded")
            .RequireAuthorization();           
    }
 
   
    
    [Authorize]
    private static async Task<IResult> GetRefundableTransactionsAsync(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();

        var refundableTransactions = await unitOfWork.Transactions.GetRefundableTransactionsAsync(
            userId, cancellationToken);

        var transactionDtos = refundableTransactions.Select(t => new
        {
            id = t.Id,
            transactionNumber = t.TransactionNumber.Value,
            amount = t.Amount.Value,
            currency = t.Amount.Currency.ToString(),
            type = t.Type.ToString(),
            description = t.Description,
            transactionDate = t.TransactionDate,
            processedAt = t.ProcessedAt,
            orderContext = t.OrderContext
        });

        return Results.Ok(new
        {
            success = true,
            refundableTransactions = transactionDtos
        });
    }
}