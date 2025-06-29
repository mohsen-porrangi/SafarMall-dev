using BuildingBlocks.CQRS;
using BuildingBlocks.Exceptions;
using BuildingBlocks.ValueObjects;
using Microsoft.Extensions.Logging;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Command.Transactions.ProcessDeposit;

/// <summary>
/// Handler for processing deposit from payment gateway
/// SOLID: Single responsibility - only handles deposit processing
/// </summary>
public class ProcessDepositHandler : ICommandHandler<ProcessDepositCommand, ProcessDepositResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessDepositHandler> _logger;

    public ProcessDepositHandler(
        IUnitOfWork unitOfWork,
        ILogger<ProcessDepositHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProcessDepositResponse> Handle(
        ProcessDepositCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing deposit for GatewayReference: {GatewayRef}, Amount: {Amount}",
                request.GatewayReference, request.Amount);

            // Find wallet by user from event
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(
                request.UserId, // حالا UserId داریم
                cancellationToken);

            if (wallet == null)
            {
                _logger.LogError("Wallet not found for GatewayReference: {GatewayRef}", request.GatewayReference);
                return new ProcessDepositResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "کیف پول کاربر یافت نشد"
                };
            }

            // Get or create currency account
            var currencyAccount = wallet.GetCurrencyAccount(request.Currency);
            if (currencyAccount == null)
            {
                currencyAccount = wallet.CreateCurrencyAccount(request.Currency);
            }

            // Create deposit transaction
            var money = Money.Create(request.Amount, request.Currency);
            var transaction = currencyAccount.CreateDepositTransaction(
                money,
                request.Description,
                request.PaymentReferenceId);

            // Add transaction to repository
            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);

            // Process the deposit (update balance)
            currencyAccount.ProcessDeposit(transaction);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deposit processed successfully. TransactionId: {TransactionId}, NewBalance: {Balance}",
                transaction.Id, currencyAccount.Balance.Value);

            return new ProcessDepositResponse
            {
                IsSuccess = true,
                TransactionId = transaction.Id,
                NewBalance = currencyAccount.Balance.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for GatewayReference: {GatewayRef}", request.GatewayReference);

            return new ProcessDepositResponse
            {
                IsSuccess = false,
                ErrorMessage = "خطا در پردازش واریز"
            };
        }
    }
}