//using BuildingBlocks.Enums;

//namespace BuildingBlocks.Contracts.Services;

///// <summary>
///// Wallet Service client interface for payment processing
///// </summary>
//public interface IWalletService
//{
//    /// <summary>
//    /// Process integrated purchase (wallet + gateway if needed)
//    /// </summary>
//    /// <param name="request">Integrated purchase request</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Purchase result with payment details</returns>
//    Task<IntegratedPurchaseResult> IntegratedPurchaseAsync(
//        IntegratedPurchaseRequest request,
//        CancellationToken cancellationToken = default);

//    /// <summary>
//    /// Check if user can afford specific amount
//    /// </summary>
//    /// <param name="userId">User identifier</param>
//    /// <param name="amount">Required amount</param>
//    /// <param name="currency">Currency code</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Affordability check result</returns>
//    Task<AffordabilityResult> CheckAffordabilityAsync(
//        Guid userId,
//        decimal amount,
//        CurrencyCode currency = CurrencyCode.IRR,
//        CancellationToken cancellationToken = default);

//    /// <summary>
//    /// Get user wallet status
//    /// </summary>
//    /// <param name="userId">User identifier</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Wallet status information</returns>
//    Task<WalletStatusResult> GetWalletStatusAsync(
//        Guid userId,
//        CancellationToken cancellationToken = default);
//}

///// <summary>
///// Integrated purchase request
///// </summary>
//public record IntegratedPurchaseRequest
//{
//    public Guid UserId { get; init; }
//    public decimal TotalAmount { get; init; }
//    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;
//    public PaymentGatewayType PaymentGateway { get; init; } = PaymentGatewayType.ZarinPal;
//    public string OrderId { get; init; } = string.Empty;
//    public string Description { get; init; } = string.Empty;
//    public bool UseCredit { get; init; } = false;
//}

///// <summary>
///// Integrated purchase result from Wallet Service
///// </summary>
//public record IntegratedPurchaseResult
//{
//    public bool IsSuccessful { get; init; }
//    public PurchaseType PurchaseType { get; init; }
//    public decimal TotalAmount { get; init; }
//    public decimal WalletBalance { get; init; }
//    public decimal RequiredPayment { get; init; }
//    public Guid? PurchaseTransactionId { get; init; }
//    public Guid? PaymentTransactionId { get; init; }
//    public string? PaymentUrl { get; init; }
//    public string? Authority { get; init; }
//    public string? ErrorMessage { get; init; }
//    public DateTime? ProcessedAt { get; init; }
//}

///// <summary>
///// Affordability check result
///// </summary>
//public record AffordabilityResult
//{
//    public bool CanAfford { get; init; }
//    public string? Reason { get; init; }
//    public decimal AvailableBalance { get; init; }
//    public decimal RequiredAmount { get; init; }
//    public decimal Shortfall { get; init; }
//}

///// <summary>
///// Wallet status result
///// </summary>
//public record WalletStatusResult
//{
//    public bool HasWallet { get; init; }
//    public bool IsActive { get; init; }
//    public decimal TotalBalanceInIrr { get; init; }
//    public bool CanMakePayment { get; init; }
//}