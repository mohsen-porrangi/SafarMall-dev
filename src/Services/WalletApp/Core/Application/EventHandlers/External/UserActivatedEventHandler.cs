using System.Diagnostics;
using BuildingBlocks.Messaging.Events.UserEvents;
using BuildingBlocks.Messaging.Handlers;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;

namespace WalletApp.Application.EventHandlers.External
{
    /// <summary>
    /// Enhanced handler with comprehensive timing logs following SOLID principles
    /// Single Responsibility: Only handles user activation for wallet creation
    /// </summary>
    public class UserActivatedEventHandler : IIntegrationEventHandler<UserActivatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserActivatedEventHandler> _logger;

        public UserActivatedEventHandler(
            IMediator mediator,
            ILogger<UserActivatedEventHandler> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handle user activation event and create wallet
        /// KISS: Simple, focused implementation
        /// </summary>
        public async Task HandleAsync(UserActivatedEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
            {
                _logger.LogWarning("Received null UserActivatedEvent");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Processing user activation for user {UserId} [Correlation: {CorrelationId}]",
                @event.UserId, @event.CorrelationId ?? "N/A");

            try
            {
                var command = new CreateWalletCommand
                {
                    UserId = @event.UserId,
                    CreateDefaultAccount = true
                };

                var result = await _mediator.Send(command, cancellationToken);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Wallet created successfully for user {UserId}. WalletId: {WalletId} [Correlation: {CorrelationId}] in {ElapsedMs}ms",
                    @event.UserId, result.WalletId, @event.CorrelationId ?? "N/A", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Failed to create wallet for activated user {UserId} [Correlation: {CorrelationId}] after {ElapsedMs}ms",
                    @event.UserId, @event.CorrelationId ?? "N/A", stopwatch.ElapsedMilliseconds);

                // YAGNI: Don't add complex retry logic here - let infrastructure handle it
                // Following fail-fast principle for better observability
            }
        }
    }
}