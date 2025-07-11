using WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;

namespace WalletApp.API.Endpoints.Wallets;

/// <summary>
/// Create wallet endpoint
/// </summary>
public class CreateWalletEndpoint : ICarterModule
{    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets", async (
            CreateWalletCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("CreateWallet")
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithSummary("Create new wallet")
            .WithDescription("Creates a new wallet for the current user with default IRR account");            
    }
}