//TODO credit implement
//using WalletApp.Domain.Common.Contracts;
//using WalletApp.Domain.Enums;

//namespace WalletApp.Application.Features.Command.Credit.SettleCredit;

///// <summary>
///// Handler for settling B2B credit
///// SOLID: Single responsibility - only handles credit settlement
///// </summary>
//public class SettleCreditHandler : ICommandHandler<SettleCreditCommand, SettleCreditResponse>
//{
//    private readonly IUnitOfWork _unitOfWork;
//    private readonly ILogger<SettleCreditHandler> _logger;

//    public SettleCreditHandler(
//        IUnitOfWork unitOfWork,
//        ILogger<SettleCreditHandler> logger)
//    {
//        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    public async Task<SettleCreditResponse> Handle(
//        SettleCreditCommand request,
//        CancellationToken cancellationToken)
//    {
//        try
//        {
//            _logger.LogInformation("Settling credit: {CreditId}", request.CreditId);

//            // Find credit
//            var credit = await _unitOfWork.Credits.GetByIdAsync(request.CreditId, cancellationToken: cancellationToken);
//            if (credit == null)
//            {
//                _logger.LogWarning("Credit not found: {CreditId}", request.CreditId);
//                return new SettleCreditResponse
//                {
//                    IsSuccess = false,
//                    ErrorMessage = "اعتبار یافت نشد"
//                };
//            }

//            // Check if already settled
//            if (credit.Status == CreditStatus.Settled)
//            {
//                _logger.LogInformation("Credit already settled: {CreditId}", request.CreditId);
//                return new SettleCreditResponse
//                {
//                    IsSuccess = true,
//                    SettledAt = credit.SettledDate,
//                    SettledAmount = credit.UsedCredit.Value
//                };
//            }

//            // Check if overdue and force flag
//            if (credit.IsOverdue() && !request.ForceSettle)
//            {
//                _logger.LogWarning("Credit is overdue and force settle not specified: {CreditId}", request.CreditId);
//                return new SettleCreditResponse
//                {
//                    IsSuccess = false,
//                    ErrorMessage = "اعتبار سررسید گذشته است. برای تسویه اجباری از پرچم ForceSettle استفاده کنید"
//                };
//            }

//            // Create settlement transaction if payment received
//            Guid settlementTransactionId;
//            if (request.SettlementTransactionId.HasValue)
//            {
//                settlementTransactionId = request.SettlementTransactionId.Value;
//            }
//            else
//            {
//                // TODO: Create settlement transaction
//                // For now, use a placeholder
//                settlementTransactionId = Guid.NewGuid();
//                _logger.LogInformation("Created settlement transaction: {TransactionId} for credit: {CreditId}",
//                    settlementTransactionId, request.CreditId);
//            }

//            // Settle the credit
//            credit.Settle(settlementTransactionId);

//            // Save changes
//            await _unitOfWork.SaveChangesAsync(cancellationToken);

//            _logger.LogInformation(
//                "Credit settled successfully: CreditId: {CreditId}, Amount: {Amount}, TransactionId: {TransactionId}",
//                request.CreditId, credit.UsedCredit.Value, settlementTransactionId);

//            return new SettleCreditResponse
//            {
//                IsSuccess = true,
//                SettledAt = credit.SettledDate,
//                SettledAmount = credit.UsedCredit.Value
//            };
//        }
//        catch (InvalidOperationException ex)
//        {
//            _logger.LogWarning(ex, "Business rule violation settling credit: {CreditId}", request.CreditId);
//            return new SettleCreditResponse
//            {
//                IsSuccess = false,
//                ErrorMessage = ex.Message
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error settling credit: {CreditId}", request.CreditId);
//            return new SettleCreditResponse
//            {
//                IsSuccess = false,
//                ErrorMessage = "خطا در تسویه اعتبار"
//            };
//        }
//    }
//}