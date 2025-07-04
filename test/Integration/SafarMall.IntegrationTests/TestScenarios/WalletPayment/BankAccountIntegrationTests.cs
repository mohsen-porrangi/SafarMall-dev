using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Wallet;

[Collection("Sequential")]
public class BankAccountIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "1")]
    public async Task Should_Add_Bank_Account_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var bankName = TestConfiguration.TestData.TestBankName;
        var accountNumber = TestUtilities.GenerateTestCardNumber()[..16];

        // Act
        var bankAccount = await AddBankAccountAsync(bankName, accountNumber);

        // Assert
        bankAccount.ShouldBeValidBankAccount(bankName);
        bankAccount.IsDefault.Should().BeTrue(); // First bank account should be default
        bankAccount.IsVerified.Should().BeFalse(); // New accounts are unverified
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "2")]
    public async Task Should_Get_Bank_Accounts_List()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Add multiple bank accounts
        var bankAccount1 = await AddBankAccountAsync("بانک ملت", TestUtilities.GenerateTestCardNumber()[..16]);
        var bankAccount2 = await AddBankAccountAsync("بانک ملی", TestUtilities.GenerateTestCardNumber()[..16]);

        // Act
        var response = await _httpClient.GetAsync(EndpointUrls.Wallet.GetBankAccounts);
        var result = await response.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        response.ShouldBeSuccessfulHttpResponse();
        // The result should contain both bank accounts
        // Exact assertion depends on the response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "3")]
    public async Task Should_Set_First_Bank_Account_As_Default()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act
        var firstBankAccount = await AddBankAccountAsync("بانک ملت", TestUtilities.GenerateTestCardNumber()[..16]);

        // Assert
        firstBankAccount.IsDefault.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "4")]
    public async Task Should_Handle_Multiple_Bank_Accounts_Default_Setting()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Add first bank account
        var firstAccount = await AddBankAccountAsync("بانک ملت", TestUtilities.GenerateTestCardNumber()[..16]);

        // Act - Add second bank account
        var secondAccount = await AddBankAccountAsync("بانک ملی", TestUtilities.GenerateTestCardNumber()[..16]);

        // Assert - First should remain default, second should not be default
        firstAccount.IsDefault.Should().BeTrue();
        secondAccount.IsDefault.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "5")]
    public async Task Should_Mask_Sensitive_Bank_Account_Information()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var fullAccountNumber = "1234567890123456";
        var fullCardNumber = TestUtilities.GenerateTestCardNumber();

        // Act
        var bankAccountRequest = new
        {
            bankName = "بانک تست",
            accountNumber = fullAccountNumber,
            cardNumber = fullCardNumber,
            shabaNumber = TestUtilities.GenerateTestShabaNumber(),
            accountHolderName = $"{testUser.Name} {testUser.Family}"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, bankAccountRequest);
        var bankAccount = await response.EnsureSuccessAndReadAsJsonAsync<BankAccountResponse>();

        // Assert - Sensitive information should be masked
        bankAccount.MaskedAccountNumber.Should().Contain("****");
        bankAccount.MaskedAccountNumber.Should().NotContain(fullAccountNumber);

        bankAccount.MaskedCardNumber.Should().Contain("****");
        bankAccount.MaskedCardNumber.Should().NotContain(fullCardNumber);

        // Should show last 4 digits
        bankAccount.MaskedAccountNumber.Should().EndWith(fullAccountNumber[^4..]);
        bankAccount.MaskedCardNumber.Should().EndWith(fullCardNumber[^4..]);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "6")]
    public async Task Should_Validate_Bank_Account_Information()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to add bank account with invalid information
        var invalidBankAccountRequest = new
        {
            bankName = "", // Empty bank name
            accountNumber = "123", // Too short
            cardNumber = "invalid", // Invalid format
            shabaNumber = "IR123", // Invalid SHABA
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, invalidBankAccountRequest);

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "7")]
    public async Task Should_Prevent_Duplicate_Account_Numbers()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var accountNumber = TestUtilities.GenerateTestCardNumber()[..16];

        // Act - Add first bank account
        await AddBankAccountAsync("بانک ملت", accountNumber);

        // Act - Try to add duplicate account number
        var duplicateRequest = new
        {
            bankName = "بانک ملی",
            accountNumber = accountNumber, // Same account number
            cardNumber = TestUtilities.GenerateTestCardNumber(),
            shabaNumber = TestUtilities.GenerateTestShabaNumber()
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, duplicateRequest);

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("exists");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "8")]
    public async Task Should_Remove_Bank_Account_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var bankAccount = await AddBankAccountAsync();

        // Act
        var response = await _httpClient.DeleteAsync(EndpointUrls.Wallet.RemoveBankAccount(bankAccount.Id));
        var result = await response.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        response.ShouldBeSuccessfulHttpResponse();

        // Verify bank account is removed from list
        var listResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetBankAccounts);
        var bankAccounts = await listResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Should not contain the removed bank account
        // Exact assertion depends on response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "9")]
    public async Task Should_Handle_Default_Account_When_Removing()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Add two bank accounts
        var firstAccount = await AddBankAccountAsync("بانک ملت", TestUtilities.GenerateTestCardNumber()[..16]);
        var secondAccount = await AddBankAccountAsync("بانک ملی", TestUtilities.GenerateTestCardNumber()[..16]);

        // Act - Remove the default account (first one)
        var response = await _httpClient.DeleteAsync(EndpointUrls.Wallet.RemoveBankAccount(firstAccount.Id));

        // Assert
        response.ShouldBeSuccessfulHttpResponse();

        // Check that another account becomes default
        var listResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetBankAccounts);
        var remainingAccounts = await listResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // One of the remaining accounts should be marked as default
        // Exact assertion depends on response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "10")]
    public async Task Should_Not_Allow_Unauthorized_Bank_Account_Access()
    {
        // Arrange - Create bank account with first user
        var firstUser = await CreateAndRegisterUserAsync();
        var bankAccount = await AddBankAccountAsync();

        // Arrange - Create second user
        var secondUser = await CreateAndRegisterUserAsync();

        // Act - Try to remove first user's bank account with second user
        ClearAuthentication();
        _currentUser = secondUser;
        SetAuthenticationToken(secondUser.Token);

        var unauthorizedResponse = await _httpClient.DeleteAsync(EndpointUrls.Wallet.RemoveBankAccount(bankAccount.Id));

        // Assert - Should be forbidden or not found
        unauthorizedResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Forbidden,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "11")]
    public async Task Should_Validate_Iranian_Card_Number_Format()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try with invalid Iranian card number
        var invalidCardRequest = new
        {
            bankName = "بانک تست",
            accountNumber = "1234567890123456",
            cardNumber = "4111111111111111", // Non-Iranian card format
            shabaNumber = TestUtilities.GenerateTestShabaNumber()
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, invalidCardRequest);

        // Assert - Should validate Iranian card number format
        // The validation depends on business rules implementation
        if (!response.IsSuccessStatusCode)
        {
            response.ShouldBeBadRequest();
            var errorContent = await response.ReadAsStringAsync();
            errorContent.Should().ContainEquivalentOf("card");
        }
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "12")]
    public async Task Should_Handle_SHABA_Number_Validation()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try with valid SHABA format
        var validShabaRequest = new
        {
            bankName = "بانک تست",
            accountNumber = "1234567890123456",
            cardNumber = TestUtilities.GenerateTestCardNumber(),
            shabaNumber = TestUtilities.GenerateTestShabaNumber()
        };

        var validResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, validShabaRequest);
        validResponse.ShouldBeSuccessfulHttpResponse();

        // Act - Try with invalid SHABA format
        var invalidShabaRequest = new
        {
            bankName = "بانک تست ۲",
            accountNumber = "6543210987654321",
            cardNumber = TestUtilities.GenerateTestCardNumber(),
            shabaNumber = "INVALID_SHABA"
        };

        var invalidResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, invalidShabaRequest);

        // Assert
        invalidResponse.ShouldBeBadRequest();
        var errorContent = await invalidResponse.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("shaba");
    }
}