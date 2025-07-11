using WalletApp.Application.Features.Command.Transactions.ProcessPaymentCallback;

namespace WalletApp.API.Endpoints.Internal;

public class ProcessPaymentCallbackEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/payment-callback", async (
            ProcessPaymentCallbackCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ProcessPaymentCallback")
        .WithTags("Internal")
        .AllowAnonymous();
    }
}