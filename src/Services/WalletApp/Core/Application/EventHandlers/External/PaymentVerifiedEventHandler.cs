using BuildingBlocks.Messaging.Events.PaymentEvents;
using BuildingBlocks.Messaging.Handlers;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Features.Command.Transactions.ProcessDeposit;

namespace WalletApp.Application.EventHandlers.External;

/// <summary>
/// Handler for payment verification events from PaymentGateway
/// SOLID: Single responsibility - only handles wallet charging after payment verification
/// </summary>
public class PaymentVerifiedEventHandler : IIntegrationEventHandler<PaymentVerifiedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentVerifiedEventHandler> _logger;

    public PaymentVerifiedEventHandler(
        IMediator mediator,
        ILogger<PaymentVerifiedEventHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(PaymentVerifiedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            _logger.LogWarning("Received null PaymentVerifiedEvent");
            return;
        }

        _logger.LogInformation(
            "Processing payment verification for PaymentId: {PaymentId}, Amount: {Amount}, GatewayRef: {GatewayRef}",
            @event.PaymentId, @event.Amount, @event.GatewayReference);

        try
        {
            // Create deposit transaction command
            var command = new ProcessDepositCommand
            {
                UserId = @event.UserId, // اضافه شده
                GatewayReference = @event.GatewayReference,
                Amount = @event.Amount,
                Currency = BuildingBlocks.Enums.CurrencyCode.IRR, // Default to IRR for now
                Description = $"شارژ کیف پول - شماره پرداخت: {@event.PaymentId}",
                PaymentReferenceId = @event.TransactionId,
                OrderContext = @event.OrderContext
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Wallet charged successfully for PaymentId: {PaymentId}, TransactionId: {TransactionId}, Amount: {Amount}",
                    @event.PaymentId, result.TransactionId, @event.Amount);
            }
            else
            {
                _logger.LogError(
                    "Failed to charge wallet for PaymentId: {PaymentId}, Error: {Error}",
                    @event.PaymentId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment verification for PaymentId: {PaymentId}",
                @event.PaymentId);

            // YAGNI: Simple error handling - infrastructure retry will handle failures
            // در آینده می‌توان Dead Letter Queue اضافه کرد
        }
    }
}