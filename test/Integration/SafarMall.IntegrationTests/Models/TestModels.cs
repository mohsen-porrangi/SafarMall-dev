namespace SafarMall.IntegrationTests.Models;

// Test User Models
public class TestUser
{
    public Guid Id { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
}

public class TestWallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class TestBankAccount
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string? ShabaNumber { get; set; }
    public bool IsVerified { get; set; }
    public bool IsDefault { get; set; }
}

public class TestTransaction
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IRR";
    public string Direction { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TestOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PassengerCount { get; set; }
    public bool HasReturn { get; set; }
    public List<TestPassenger> Passengers { get; set; } = new();
}

public class TestPassenger
{
    public long Id { get; set; }
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameFa { get; set; } = string.Empty;
    public string LastNameFa { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int Gender { get; set; }
    public bool IsIranian { get; set; }
    public string? NationalCode { get; set; }
    public string? PassportNo { get; set; }
}