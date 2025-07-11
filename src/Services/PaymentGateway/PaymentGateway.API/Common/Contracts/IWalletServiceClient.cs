using BuildingBlocks.Enums;

namespace PaymentGateway.API.Services;

public interface IWalletServiceClient
{
    Task<bool> ProcessPaymentCallbackAsync(
        PaymentCallbackRequest request,
        CancellationToken cancellationToken = default);
}

public record PaymentCallbackRequest(
    string Authority,
    string Status,
    decimal Amount,
    string TransactionId,
    string? TrackingCode = null);