using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalletApp.Application.Features.Query.Wallets.CheckWalletExists
{
    public record CheckWalletExistsQuery(Guid UserId) : IQuery<WalletExistsDto>;
    public record WalletExistsDto
    {
        public Guid UserId { get; init; }
        public bool HasWallet { get; init; }
    };
}
