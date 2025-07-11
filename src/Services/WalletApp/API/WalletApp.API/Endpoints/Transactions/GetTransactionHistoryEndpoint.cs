//using Carter;
//using WalletApp.Application.Features.Query.Transactions.GetTransactionHistory;

//namespace WalletApp.API.Endpoints.Transactions;

///// <summary>
///// Get transaction history endpoint
///// </summary>
//public class GetTransactionHistoryEndpoint : ICarterModule
//{
//    public void AddRoutes(IEndpointRouteBuilder app)
//    {
//        app.MapGet("/api/transactions/history", async (
//            // [AsParameters] GetTransactionHistoryQuery query,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var query = new GetTransactionHistoryQuery();
//            var result = await sender.Send(query, ct);
//            return Results.Ok(result);
//        })
//            .WithName("GetTransactionHistory")
//            .WithTags("Transactions")
//            .WithDescription("Get paginated transaction history for current user")
//            .WithSummary("Get transaction history")
//            .RequireAuthorization();           
//    }
//}