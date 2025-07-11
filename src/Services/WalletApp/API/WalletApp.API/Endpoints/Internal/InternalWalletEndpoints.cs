using Carter;
using MediatR;
using WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;
using WalletApp.Application.Features.Query.Wallets.CheckWalletExists;

namespace WalletApp.API.Endpoints.Internal;

/// <summary>
/// Internal wallet endpoints for service-to-service communication
/// </summary>
public class InternalWalletEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Internal endpoints don't require user authentication
        var internalGroup = app.MapGroup("/api/internal/wallets")
            .WithTags("Internal-Wallets");       

        internalGroup.MapGet("/{userId:guid}/exists", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new CheckWalletExistsQuery(userId);
            var result = await sender.Send(query, ct);

            return Results.Ok(new
            {
                userId = result.UserId,
                hasWallet = result.HasWallet
            });
        })
            .WithName("CheckWalletExists")
            .WithSummary("Check wallet exists (Internal)")
            .WithDescription("Check if user has a wallet");
    }
    
}