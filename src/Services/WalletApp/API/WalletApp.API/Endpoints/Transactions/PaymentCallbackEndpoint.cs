using WalletApp.Application.Features.Command.Transactions.ProcessPaymentCallback;

namespace WalletApp.API.Endpoints.Transactions;

/// <summary>
/// Payment callback endpoint
/// </summary>
public class PaymentCallbackEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/payment-callback", async (
            ProcessPaymentCallbackCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        }
            )
            .WithName("PaymentCallback")
            .WithTags("Transactions")
            .AllowAnonymous() // Payment gateways call this endpoint
            .WithDescription("Handle payment gateway callback for transaction verification")
            .WithSummary("Payment gateway callback");            

        // GET endpoint for redirect-based callbacks
        //app.MapGet("/api/transactions/payment-callback", PaymentCallbackGetAsync)
        //    .WithName("PaymentCallbackGet")
        //    .WithTags("Transactions")
        //    .AllowAnonymous()
        //    .WithOpenApi();
    }

    //public record PaymentCallbackRequest(
    //    string Authority,
    //    string Status,
    //    decimal? Amount = null
    //);

    //private static async Task<IResult> PaymentCallbackAsync(
    //    PaymentCallbackRequest request,
    //    IMediator mediator,
    //    CancellationToken cancellationToken)
    //{
    //    var command = new ProcessPaymentCallbackCommand
    //    {
    //        Authority = request.Authority,
    //        Status = request.Status,
    //        Amount = request.Amount
    //    };

    //    var result = await mediator.Send(command, cancellationToken);

    //    if (!result.IsSuccessful)
    //    {
    //        return Results.BadRequest(new
    //        {
    //            error = result.ErrorMessage,
    //            transactionId = result.TransactionId
    //        });
    //    }

    //    return Results.Ok(new
    //    {
    //        isSuccessful = result.IsSuccessful,
    //        isVerified = result.IsVerified,
    //        transactionId = result.TransactionId,
    //        walletId = result.WalletId,
    //        amount = result.Amount,
    //        currency = result.Currency?.ToString(),
    //        newBalance = result.NewBalance,
    //        referenceId = result.ReferenceId,
    //        processedAt = result.ProcessedAt
    //    });
    //}

    //private static async Task<IResult> PaymentCallbackGetAsync(
    //    string Authority,
    //    string Status,
    //    decimal? Amount,
    //    IMediator mediator,
    //    CancellationToken cancellationToken)
    //{
    //    var request = new PaymentCallbackRequest(Authority, Status, Amount);
    //    return await PaymentCallbackAsync(request, mediator, cancellationToken);
    //}
}