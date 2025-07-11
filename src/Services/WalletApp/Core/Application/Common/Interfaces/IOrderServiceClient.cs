using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalletApp.Application.Common.Interfaces;

public interface IOrderServiceClient
{
    Task<bool> CompleteOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default);
}

public record CompleteOrderRequest(
    OrderStatus Status = OrderStatus.Completed,
    string Reason = "Payment verified and wallet charged");
