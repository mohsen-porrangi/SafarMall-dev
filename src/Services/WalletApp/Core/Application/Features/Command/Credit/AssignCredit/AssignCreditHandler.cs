using BuildingBlocks.CQRS;
using BuildingBlocks.Exceptions;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletApp.Domain.Common;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Command.Credit.AssignCredit;

/// <summary>
/// Handler for assigning credit to B2B customers
/// SOLID: Single responsibility - only handles credit assignment
/// </summary>
public class AssignCreditHandler(IUnitOfWork unitOfWork, ILogger<AssignCreditHandler> logger)
    : ICommandHandler<AssignCreditCommand, AssignCreditResponse>
{
    public async Task<AssignCreditResponse> Handle(
        AssignCreditCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Assigning credit to user {UserId}: Amount: {Amount} {Currency}, Due: {DueDate}",
                request.UserId, request.CreditAmount, request.Currency, request.DueDate);

            // Find user's wallet using optimized query
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.UserId && !w.IsDeleted,
                    q => q.Include(w => w.Credits),
                    track: true,
                    cancellationToken);

            if (wallet == null)
            {
                logger.LogWarning("Wallet not found for user: {UserId}", request.UserId);
                return new AssignCreditResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "کیف پول کاربر یافت نشد"
                };
            }

            // Check if user already has active credit
            var existingCredit = wallet.GetActiveCredit();
            if (existingCredit != null)
            {
                logger.LogWarning(
                    "User {UserId} already has active credit: {CreditId}",
                    request.UserId, existingCredit.Id);

                return new AssignCreditResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "کاربر دارای اعتبار فعال است"
                };
            }

            // Validate business rules
            var creditMoney = Money.Create(request.CreditAmount, request.Currency);
            if (!BusinessRules.Credit.IsValidCreditAmount(creditMoney))
            {
                return new AssignCreditResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "مبلغ اعتبار نامعتبر است"
                };
            }

            if (!BusinessRules.Credit.IsValidCreditDueDate(request.DueDate))
            {
                return new AssignCreditResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "تاریخ سررسید اعتبار نامعتبر است"
                };
            }

            // Build description with company info
            var description = BuildCreditDescription(request);

            // Assign credit to wallet
            wallet.AssignCredit(request.CreditAmount, request.DueDate, description);

            // Save changes
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var newCredit = wallet.GetActiveCredit();

            logger.LogInformation(
                "Credit assigned successfully: UserId: {UserId}, CreditId: {CreditId}, Amount: {Amount}",
                request.UserId, newCredit!.Id, request.CreditAmount);

            return new AssignCreditResponse
            {
                IsSuccess = true,
                CreditId = newCredit.Id,
                AssignedAmount = request.CreditAmount,
                DueDate = request.DueDate
            };
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid credit assignment request for user: {UserId}", request.UserId);
            return new AssignCreditResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation for user: {UserId}", request.UserId);
            return new AssignCreditResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning credit to user: {UserId}", request.UserId);
            return new AssignCreditResponse
            {
                IsSuccess = false,
                ErrorMessage = "خطا در تخصیص اعتبار"
            };
        }
    }

    /// <summary>
    /// Build comprehensive credit description
    /// </summary>
    private static string BuildCreditDescription(AssignCreditCommand request)
    {
        var parts = new List<string> { request.Description };

        if (!string.IsNullOrWhiteSpace(request.CompanyName))
            parts.Add($"شرکت: {request.CompanyName}");

        if (!string.IsNullOrWhiteSpace(request.ReferenceNumber))
            parts.Add($"شماره مرجع: {request.ReferenceNumber}");

        return string.Join(" - ", parts);
    }
}