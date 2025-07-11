using BuildingBlocks.Enums;
using Carter;
using MediatR;
using WalletApp.Application.Features.Query.Wallets.GetUserWalletBalance;
using WalletApp.Application.Features.Query.Wallets.GetWalletStatus;
using WalletApp.Application.Features.Query.Wallets.CheckAffordability;

namespace WalletApp.API.Endpoints.Internal;

/// <summary>
/// Internal wallet balance endpoints for Order service
/// </summary>
public class InternalWalletBalanceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var internalGroup = app.MapGroup("/api/internal/wallets")
            .WithTags("Internal-Wallets");

        internalGroup.MapGet("/{userId:guid}/balance", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetUserWalletBalanceQuery(userId);
            var result = await sender.Send(query, ct);

            return Results.Ok(new
            {
                success = true,
                hasWallet = true,
                totalBalanceInIrr = result.TotalBalanceInIrr,
                currencyBalances = result.CurrencyBalances.Select(cb => new
                {
                    currency = cb.Currency.ToString(),
                    balance = cb.Balance
                })
            });
        })
            .WithName("GetWalletBalanceInternal")
            .WithDescription("Get user wallet balance for Order service")
            .WithSummary("Get wallet balance (Internal)");

        internalGroup.MapGet("/{userId:guid}/status", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetWalletStatusQuery(userId);
            var result = await sender.Send(query, ct);

            return Results.Ok(new
            {
                success = true,
                hasWallet = result.HasWallet,
                isActive = result.IsActive,
                totalBalanceInIrr = result.TotalBalanceInIrr,
                canMakePayment = result.CanMakePayment
            });
        })
            .WithName("GetWalletStatusInternal")
            .WithDescription("Get comprehensive wallet status for Order service")
            .WithSummary("Get wallet status (Internal)");

        internalGroup.MapPost("/{userId:guid}/check-affordability", async (
            Guid userId,
            CheckAffordabilityRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var currency = Enum.TryParse<CurrencyCode>(request.Currency, out var parsed)
                ? parsed
                : CurrencyCode.IRR;

            var query = new CheckAffordabilityQuery(userId, request.Amount, currency);
            var result = await sender.Send(query, ct);

            return Results.Ok(new
            {
                success = true,
                canAfford = result.CanAfford,
                reason = result.Reason,
                availableBalance = result.AvailableBalance,
                requiredAmount = result.RequiredAmount,
                shortfall = result.Shortfall
            });
        })
            .WithName("CheckAffordabilityInternal")
            .WithDescription("Check if user can afford specific amount")
            .WithSummary("Check affordability (Internal)");
    }

    public record CheckAffordabilityRequest(
        decimal Amount,
        string Currency = "IRR"
    );
}