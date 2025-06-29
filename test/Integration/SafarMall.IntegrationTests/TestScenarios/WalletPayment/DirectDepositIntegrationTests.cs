using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Wallet;

[Collection("Sequential")]
public class DirectDepositIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "1")]
    public async Task Should_Initiate_Direct_Deposit_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 100000m;
        var description = "Test direct deposit";

        // Act
        var depositResponse = await PerformDirectDepositAsync(depositAmount, description);

        // Assert
        depositResponse.ShouldBeValidDepositResponse();
        depositResponse.Authority.Should().NotBeNullOrEmpty();
        depositResponse.PaymentUrl.Should().StartWith("http");
        depositResponse.PendingTransactionId.Should().NotBeNull();
        depositResponse.PendingTransactionId.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "2")]
    public async Task Should_Complete_Deposit_Via_Successful_Callback()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 150000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Step 1: Initiate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);

        // Act - Step 2: Complete via successful callback
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, depositAmount);

        // Assert - Balance should be updated
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(initialBalance + depositAmount);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "3")]
    public async Task Should_Handle_Failed_Deposit_Callback()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 75000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Step 1: Initiate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);

        // Act - Step 2: Simulate failed callback
        var failedCallbackRequest = new
        {
            authority = depositResponse.Authority,
            status = "FAILED",
            amount = depositAmount
        };

        var callbackResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, failedCallbackRequest);

        // Assert - Callback should indicate failure
        callbackResponse.ShouldBeBadRequest();

        // Assert - Balance should remain unchanged
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(initialBalance);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "4")]
    public async Task Should_Reject_Deposit_With_Invalid_Amount()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act & Assert - Zero amount
        var zeroAmountRequest = new
        {
            amount = 0m,
            description = "Invalid zero amount",
            callbackUrl = "https://test.callback.url"
        };

        var zeroResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.DirectDeposit, zeroAmountRequest);
        zeroResponse.ShouldBeBadRequest();

        // Act & Assert - Negative amount
        var negativeAmountRequest = new
        {
            amount = -5000m,
            description = "Invalid negative amount",
            callbackUrl = "https://test.callback.url"
        };

        var negativeResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.DirectDeposit, negativeAmountRequest);
        negativeResponse.ShouldBeBadRequest();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "5")]
    public async Task Should_Reject_Deposit_Without_Authentication()
    {
        // Arrange
        ClearAuthentication(); // No authentication

        // Act
        var depositRequest = new
        {
            amount = 50000m,
            description = "Unauthorized deposit attempt",
            callbackUrl = "https://test.callback.url"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.DirectDeposit, depositRequest);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "6")]
    public async Task Should_Handle_Multiple_Concurrent_Deposits()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 50000m;
        var numberOfDeposits = 3;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Initiate multiple deposits concurrently
        var depositTasks = Enumerable.Range(1, numberOfDeposits)
            .Select(i => PerformDirectDepositAsync(depositAmount, $"Concurrent deposit {i}"))
            .ToArray();

        var depositResponses = await Task.WhenAll(depositTasks);

        // Complete all deposits via callbacks
        var callbackTasks = depositResponses
            .Select(response => CompleteDepositViaCallbackAsync(response.Authority!, depositAmount))
            .ToArray();

        await Task.WhenAll(callbackTasks);

        // Assert - All deposits should be successful
        depositResponses.Should().AllSatisfy(response => response.ShouldBeValidDepositResponse());

        // Assert - Final balance should reflect all deposits
        var expectedBalance = initialBalance + (depositAmount * numberOfDeposits);
        await AssertWalletBalanceAsync(expectedBalance, "multiple concurrent deposits");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "7")]
    public async Task Should_Handle_Large_Deposit_Amount()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var largeDepositAmount = 5000000m; // 5 million
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act
        var depositResponse = await PerformDirectDepositAsync(largeDepositAmount, "Large amount test deposit");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, largeDepositAmount);

        // Assert
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(initialBalance + largeDepositAmount);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "8")]
    public async Task Should_Reject_Callback_With_Mismatched_Authority()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 80000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Initiate legitimate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);

        // Act - Try callback with wrong authority
        var invalidCallbackRequest = new
        {
            authority = "FAKE_AUTHORITY_12345", // Wrong authority
            status = "OK",
            amount = depositAmount
        };

        var callbackResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, invalidCallbackRequest);

        // Assert - Callback should be rejected
        callbackResponse.ShouldBeBadRequest();

        // Assert - Balance should remain unchanged
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(initialBalance);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "9")]
    public async Task Should_Reject_Callback_With_Mismatched_Amount()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 90000m;
        var wrongAmount = 120000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Initiate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);

        // Act - Try callback with wrong amount
        var invalidCallbackRequest = new
        {
            authority = depositResponse.Authority,
            status = "OK",
            amount = wrongAmount // Different amount
        };

        var callbackResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, invalidCallbackRequest);

        // Assert - Callback should be rejected
        callbackResponse.ShouldBeBadRequest();

        // Assert - Balance should remain unchanged
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(initialBalance);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "10")]
    public async Task Should_Handle_Deposit_With_Special_Characters_In_Description()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 60000m;
        var specialDescription = "تست واریز با کاراکترهای خاص: !@#$%^&*()_+{}[]|\\:;\"'<>?,./";

        // Act
        var depositResponse = await PerformDirectDepositAsync(depositAmount, specialDescription);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, depositAmount);

        // Assert
        depositResponse.ShouldBeValidDepositResponse();

        // Verify the deposit was completed successfully
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.TotalBalanceInIrr.Should().Be(depositAmount);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "11")]
    public async Task Should_Create_Transaction_Record_For_Completed_Deposit()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 110000m;

        // Act
        var depositResponse = await PerformDirectDepositAsync(depositAmount, "Transaction record test");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, depositAmount);

        // Act - Get transaction history
        var historyResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.TransactionHistory);

        // Assert
        historyResponse.ShouldBeSuccessfulHttpResponse();

        // Should contain the deposit transaction
        var historyContent = await historyResponse.ReadAsStringAsync();
        historyContent.Should().Contain("Deposit");
        historyContent.Should().Contain(depositAmount.ToString());
    }
}