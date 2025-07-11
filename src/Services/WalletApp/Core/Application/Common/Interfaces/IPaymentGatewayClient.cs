using BuildingBlocks.ValueObjects;

namespace WalletApp.Application.Common.Interfaces;

/// <summary>
/// Payment gateway client interface
/// </summary>
public interface IPaymentGatewayClient
{
    /// <summary>
    /// Create payment request
    /// </summary>
    Task<PaymentResult> CreatePaymentAsync(
        Money amount,
        string description,        
        PaymentGatewayType gateway,
        string? orderId = null,        
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment callback
    /// </summary>
    Task<PaymentVerificationResult> VerifyPaymentAsync(
        string authority,
        Money expectedAmount,
        PaymentGatewayType gateway = PaymentGatewayType.Zibal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment status
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(
        string authority,
        PaymentGatewayType gateway = PaymentGatewayType.Zibal,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment gateway types
/// </summary>
//public enum PaymentGatewayType
//{
//    ZarinPal = 1,
//    Sandbox = 99  // For testing
//}

/// <summary>
/// Payment creation result
/// </summary>
public record PaymentResult
{
    public bool IsSuccessful { get; init; }
    public string? Authority { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}

/// <summary>
/// Payment verification result
/// </summary>
public record PaymentVerificationResult
{
    public bool IsSuccessful { get; init; }
    public bool IsVerified { get; init; }
    public string? ReferenceId { get; init; }
    public Money? ActualAmount { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime? VerificationDate { get; init; }
}

/// <summary>
/// Payment status result
/// </summary>
public record PaymentStatusResult
{
    public bool IsSuccessful { get; init; }
    public PaymentStatus Status { get; init; }
    public Money? Amount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

