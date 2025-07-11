using WalletApp.Application.Features.Command.Transactions.IntegratedPurchase;

namespace WalletApp.API.Endpoints.Internal;

/// <summary>
/// Integrated purchase endpoint
/// </summary>
public class InternalIntegratedPurchaseEndpoint : ICarterModule
{    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/transactions/integrated-purchase", async (
            IntegratedPurchaseCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("internal-IntegratedPurchase")
            .WithTags("Transactions")
            .WithSummary("Integrated purchase")
            .WithDescription("Process purchase with automatic wallet top-up if needed")
            .RequireAuthorization();            
    }
}