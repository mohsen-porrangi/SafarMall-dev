using BuildingBlocks.Contracts;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.BankAccounts.GetBankAccounts;

/// <summary>
/// Get bank accounts handler
/// </summary>
public class GetBankAccountsHandler(IUnitOfWork unitOfWork, ICurrentUserService userService) : IQueryHandler<GetBankAccountsQuery, GetBankAccountsResult>
{

    public async Task<GetBankAccountsResult> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();
        var wallet = await unitOfWork.Wallets.GetByUserIdWithIncludesAsync(
            userId,
            includeCurrencyAccounts: false,
            includeBankAccounts: true,
            cancellationToken: cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(userId);
        }

        var bankAccountDtos = wallet.BankAccounts
            .Where(ba => !ba.IsDeleted)
            .OrderByDescending(ba => ba.IsDefault)
            .ThenByDescending(ba => ba.CreatedAt)
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
            });

        return new GetBankAccountsResult
        {
            BankAccounts = bankAccountDtos
        };
    }
}
