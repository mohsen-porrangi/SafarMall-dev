using WalletApp.Application.Features.Command.Transactions.TransferMoney;

namespace WalletApp.API.Endpoints.Transactions;

/// <summary>
/// Wallet transfer endpoint - انتقال بین کیف پول‌ها
/// </summary>
public class WalletTransferEndpoint : ICarterModule
{
    [Authorize]
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/transfer", async (
            TransferMoneyCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("TransferMoney")
            .WithTags("Transactions")
            .WithSummary("Transfer money between wallets")
            .WithDescription("Transfer money from current user's wallet to another user's wallet")
            .RequireAuthorization();           
    }
}