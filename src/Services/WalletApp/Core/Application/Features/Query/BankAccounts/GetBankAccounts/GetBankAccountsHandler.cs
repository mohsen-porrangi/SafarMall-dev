using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.BankAccounts.GetBankAccounts;

/// <summary>
/// Get bank accounts handler
/// </summary>
public class GetBankAccountsHandler(IUnitOfWork unitOfWork, ICurrentUserService userService)
    : IQueryHandler<GetBankAccountsQuery, GetBankAccountsResult>
{
    public async Task<GetBankAccountsResult> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();

        // Direct query with optimized includes and ordering
        var bankAccounts = await unitOfWork.Wallets
            .Query(w => w.UserId == userId && !w.IsDeleted)
            .Include(w => w.BankAccounts.Where(ba => !ba.IsDeleted))
            .OrderByDescending(w => w.BankAccounts.Max(ba => ba.IsDefault ? 1 : 0))
            .ThenByDescending(w => w.BankAccounts.Max(ba => ba.CreatedAt))
            .SelectMany(w => w.BankAccounts)
            .Select(ba => new BankAccountDto
            {
                Id = ba.Id,
                BankName = ba.BankName,
                MaskedAccountNumber = ba.GetMaskedAccountNumber(),
                MaskedCardNumber = ba.GetMaskedCardNumber(),
                ShabaNumber = ba.ShabaNumber,
                AccountHolderName = ba.AccountHolderName,
                IsVerified = ba.IsVerified,
                IsDefault = ba.IsDefault,
                IsActive = ba.IsActive,
                CreatedAt = ba.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Check if user has any wallet (empty result means no wallet)
        if (!bankAccounts.Any())
        {
            // Verify if user has a wallet at all
            var hasWallet = await unitOfWork.Wallets
                .ExistsAsync(w => w.UserId == userId && !w.IsDeleted, cancellationToken);

            if (!hasWallet)
            {
                throw new WalletNotFoundException(userId);
            }
        }

        return new GetBankAccountsResult
        {
            BankAccounts = bankAccounts
        };
    }
}