using WalletApp.Application.Features.Command.Transactions.DirectDeposit;

namespace WalletApp.API.Endpoints.Transactions;

/// <summary>
/// Direct deposit endpoint
/// </summary>
public class DirectDepositEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/direct-deposit", async (
            DirectDepositCommand command,
            ISender sender,
            CancellationToken ct
            ) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("DirectDeposit")
            .WithTags("Transactions")
            .WithSummary("Direct wallet deposit")
            .WithDescription("Create direct deposit to wallet via payment gateway")
            .RequireAuthorization();
    }
}
