using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Query.Wallets.CheckWalletExists
{
    public class CheckWalletExistsHandler(IUnitOfWork unitOfWork)
        : IQueryHandler<CheckWalletExistsQuery, WalletExistsDto>
    {
        public async Task<WalletExistsDto> Handle(CheckWalletExistsQuery request, CancellationToken cancellationToken)
        {
            // Use optimized ExistsAsync from RepositoryBase instead of custom method
            var exists = await unitOfWork.Wallets
                .ExistsAsync(w => w.UserId == request.UserId && !w.IsDeleted, cancellationToken);

            return new WalletExistsDto
            {
                UserId = request.UserId,
                HasWallet = exists
            };
        }
    }
}
