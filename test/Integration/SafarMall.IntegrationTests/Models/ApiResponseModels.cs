using System.Text.Json.Serialization;

namespace SafarMall.IntegrationTests.Models;

// User Management API Responses
public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("nextStep")]
    public string? NextStep { get; set; }
}

public class UserProfileResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("nationalId")]
    public string? NationalId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

// Wallet Payment API Responses
public class WalletResponse
{
    [JsonPropertyName("walletId")]
    public Guid WalletId { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("totalBalanceInIrr")]
    public decimal TotalBalanceInIrr { get; set; }

    [JsonPropertyName("currencyBalances")]
    public List<CurrencyBalance> CurrencyBalances { get; set; } = new();
}

public class CurrencyBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class TransactionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("transactionNumber")]
    public string TransactionNumber { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class BankAccountResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("bankName")]
    public string BankName { get; set; } = string.Empty;

    [JsonPropertyName("maskedAccountNumber")]
    public string MaskedAccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("maskedCardNumber")]
    public string MaskedCardNumber { get; set; } = string.Empty;

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

public class DirectDepositResponse
{
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccessful { get; set; }

    [JsonPropertyName("paymentUrl")]
    public string? PaymentUrl { get; set; }

    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("pendingTransactionId")]
    public Guid? PendingTransactionId { get; set; }
}

// Order API Responses
public class OrderResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("serviceType")]
    public string ServiceType { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("passengerCount")]
    public int PassengerCount { get; set; }

    [JsonPropertyName("hasReturn")]
    public bool HasReturn { get; set; }
}

public class SavedPassengerResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("firstNameEn")]
    public string FirstNameEn { get; set; } = string.Empty;

    [JsonPropertyName("lastNameEn")]
    public string LastNameEn { get; set; } = string.Empty;

    [JsonPropertyName("firstNameFa")]
    public string FirstNameFa { get; set; } = string.Empty;

    [JsonPropertyName("lastNameFa")]
    public string LastNameFa { get; set; } = string.Empty;

    [JsonPropertyName("nationalCode")]
    public string? NationalCode { get; set; }

    [JsonPropertyName("PassportNo")]
    public string? PassportNo { get; set; }
}