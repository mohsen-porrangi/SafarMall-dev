using WalletApp.Application.Features.Query.Wallets.GetUserWalletBalance;

namespace WalletApp.API.Endpoints.Admin;

public class GetUserWalletBalanceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/users/{userId:guid}/wallet/balance", async (
            [AsParameters] Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetUserWalletBalanceQuery(userId);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetUserWalletBalance")
        .WithTags("Admin-Wallets")
        .RequireAuthorization()
        .WithSummary("Get user wallet balance")
        .WithDescription("Get user's wallet balance for all currencies");


    }
}