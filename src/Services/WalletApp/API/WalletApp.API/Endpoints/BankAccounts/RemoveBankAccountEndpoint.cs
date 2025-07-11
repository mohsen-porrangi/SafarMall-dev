using Microsoft.AspNetCore.Mvc;
using WalletApp.Application.Features.Command.BankAccounts.RemoveBankAccount;

namespace WalletApp.API.Endpoints.BankAccounts;

/// <summary>
/// Remove bank account endpoint
/// </summary>
public class RemoveBankAccountEndpoint : ICarterModule
{
    [Authorize]
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/bank-accounts/", async (
            [FromBody] RemoveBankAccountCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok();
        })
            .WithName("RemoveBankAccount")
            .WithTags("Bank Accounts")
            .WithDescription("Remove a bank account from user's wallet")
            .WithSummary("Remove bank account")
            .RequireAuthorization();          
    }   
}