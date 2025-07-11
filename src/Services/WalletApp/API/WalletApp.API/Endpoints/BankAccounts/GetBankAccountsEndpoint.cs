using WalletApp.Application.Features.Query.BankAccounts.GetBankAccounts;

namespace WalletApp.API.Endpoints.BankAccounts;

/// <summary>
/// Get bank accounts endpoint
/// </summary>
public class GetBankAccountsEndpoint : ICarterModule
{
    [Authorize]
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bank-accounts", async (            
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetBankAccountsQuery();
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        }
            )
            .WithName("GetBankAccounts")
            .WithTags("Bank Accounts")
            .WithDescription("Get all bank accounts for the current user")
            .WithSummary("Get user bank accounts")
            .RequireAuthorization();           
    }   
}