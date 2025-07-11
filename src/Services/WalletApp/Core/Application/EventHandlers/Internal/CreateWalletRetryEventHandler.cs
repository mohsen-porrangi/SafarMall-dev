using BuildingBlocks.Messaging.Events.UserEvents;
using BuildingBlocks.Messaging.Handlers;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.EventHandlers.Internal;

/// <summary>
/// Handler for CreateWalletRetryEvent
/// </summary>
public class CreateWalletRetryEventHandler(
    IMediator mediator,
    ILogger<CreateWalletRetryEventHandler> logger,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<CreateWalletRetryEvent>
{
    public async Task HandleAsync(CreateWalletRetryEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            logger.LogWarning("Received null CreateWalletRetryEvent");
            return;
        }

        try
        {
            // Double-check using optimized query
            var hasWallet = await unitOfWork.Wallets
                .ExistsAsync(w => w.UserId == @event.UserId && !w.IsDeleted, cancellationToken);

            if (hasWallet)
            {
                logger.LogInformation("User {UserId} already has wallet, skipping retry", @event.UserId);
                return;
            }

            logger.LogInformation("Retrying wallet creation for user: {UserId}", @event.UserId);

            var command = new CreateWalletCommand
            {
                UserId = @event.UserId,
                CreateDefaultAccount = true
            };

            var result = await mediator.Send(command, cancellationToken);

            logger.LogInformation("Wallet retry successful for user: {UserId}, WalletId: {WalletId}",
                @event.UserId, result.WalletId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wallet retry failed for user: {UserId}", @event.UserId);
            // YAGNI: Simple fail-fast approach - infrastructure will handle retry logic
        }
    }
}