using Azure;
using WalletApp.Application.Features.Query.Wallets.GetWalletSummary;

namespace WalletApp.API.Endpoints.Wallets;

/// <summary>
/// Get wallet summary endpoint
/// </summary>
public class GetWalletSummaryEndpoint : ICarterModule
{    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets/summary", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetWalletSummaryQuery();
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
            .WithName("GetWalletSummary")
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithSummary("Get wallet summary")
            .WithDescription("Get comprehensive wallet summary including balance, transactions, and statistics");            
    }      
}