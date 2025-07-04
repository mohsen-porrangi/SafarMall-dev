using WalletApp.Application.Features.Query.Wallets.GetWalletBalance;

namespace WalletApp.API.Endpoints.Wallets;

/// <summary>
/// Get wallet balance endpoint
/// </summary>
public class GetWalletBalanceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets/balance", async (
             ISender sender,
             CancellationToken ct) =>
        {
            var query = new GetWalletBalanceQuery();
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
         .WithName("GetMyWalletBalance")
         .WithTags("Wallets")
         .RequireAuthorization()
         .WithSummary("Get my wallet balance")
         .WithDescription("Get current user's wallet balance for all currencies");         
    }
}