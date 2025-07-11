using Carter;
using MediatR;
using WalletApp.Application.Features.Command.BankAccounts.AddBankAccount;

namespace WalletApp.API.Endpoints.BankAccounts;

/// <summary>
/// Add bank account endpoint
/// </summary>
public class AddBankAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bank-accounts", async (
            AddBankAccountCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
            .WithName("AddBankAccount")
            .WithTags("Bank Accounts")
            .WithDescription("Add new bank account to user's wallet")
            .WithSummary("Add bank account")
            .RequireAuthorization();          
    }       
}