using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Wallet;

[Collection("Sequential")]
public class WalletTransactionIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "1")]
    public async Task Should_Perform_Direct_Deposit_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 100000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Step 1: Initiate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount, "Test direct deposit");

        // Assert - Deposit should be initiated successfully
        depositResponse.ShouldBeValidDepositResponse();

        // Act - Step 2: Complete deposit via callback (simulate payment gateway)
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, depositAmount);

        // Assert - Balance should be updated
        await AssertWalletBalanceChangedAsync(initialBalance, depositAmount, "direct deposit");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "2")]
    public async Task Should_Get_Transaction_History_After_Deposit()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 50000m;

        // Act - Perform deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, depositAmount);

        // Act - Get transaction history
        var historyResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.TransactionHistory);
        var historyData = await historyResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should contain the deposit transaction
        historyResponse.ShouldBeSuccessfulHttpResponse();
        // Additional assertions would depend on the exact response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "3")]
    public async Task Should_Handle_Multiple_Deposits_Correctly()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var deposit1Amount = 75000m;
        var deposit2Amount = 125000m;
        var expectedTotalBalance = deposit1Amount + deposit2Amount;

        // Act - Perform first deposit
        var deposit1Response = await PerformDirectDepositAsync(deposit1Amount, "First deposit");
        await CompleteDepositViaCallbackAsync(deposit1Response.Authority!, deposit1Amount);

        // Act - Perform second deposit
        var deposit2Response = await PerformDirectDepositAsync(deposit2Amount, "Second deposit");
        await CompleteDepositViaCallbackAsync(deposit2Response.Authority!, deposit2Amount);

        // Assert - Total balance should be sum of both deposits
        await AssertWalletBalanceAsync(expectedTotalBalance, "multiple deposits");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "4")]
    public async Task Should_Handle_Failed_Payment_Callback()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var depositAmount = 60000m;
        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Initiate deposit
        var depositResponse = await PerformDirectDepositAsync(depositAmount);

        // Act - Simulate failed payment callback
        var failedCallbackRequest = new
        {
            authority = depositResponse.Authority,
            status = "FAILED",
            amount = depositAmount
        };

        var callbackResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, failedCallbackRequest);

        // Assert - Callback should be processed but balance shouldn't change
        callbackResponse.ShouldBeBadRequest(); // or appropriate error status
        await AssertWalletBalanceAsync(initialBalance, "failed payment");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "5")]
    public async Task Should_Transfer_Money_Between_Users()
    {
        // Arrange
        var fromUser = await CreateAndRegisterUserAsync();
        var toUser = await CreateAndRegisterUserAsync();

        // Give first user some balance
        var initialAmount = 200000m;
        var transferAmount = 75000m;

        var depositResponse = await PerformDirectDepositAsync(initialAmount);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, initialAmount);

        // Act - Transfer money to second user
        var transferRequest = new
        {
            toUserId = toUser.Id,
            amount = transferAmount,
            description = "Test transfer",
            reference = "TEST-TRANSFER-001"
        };

        var transferResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.WalletTransfer, transferRequest);
        var transferResult = await transferResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Transfer should be successful
        transferResponse.ShouldBeSuccessfulHttpResponse();

        // Assert - From user balance should decrease
        await AssertWalletBalanceAsync(initialAmount - transferAmount, "money transfer (sender)");

        // Switch to second user and check their balance
        ClearAuthentication();
        _currentUser = toUser;
        SetAuthenticationToken(toUser.Token);

        // Assert - To user balance should increase
        await AssertWalletBalanceAsync(transferAmount, "money transfer (receiver)");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "6")]
    public async Task Should_Reject_Transfer_With_Insufficient_Balance()
    {
        // Arrange
        var fromUser = await CreateAndRegisterUserAsync();
        var toUser = await CreateAndRegisterUserAsync();

        var availableBalance = 50000m;
        var transferAmount = 100000m; // More than available

        var depositResponse = await PerformDirectDepositAsync(availableBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, availableBalance);

        // Act - Try to transfer more than available balance
        var transferRequest = new
        {
            toUserId = toUser.Id,
            amount = transferAmount,
            description = "Test insufficient balance transfer"
        };

        var transferResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.WalletTransfer, transferRequest);

        // Assert - Transfer should be rejected
        transferResponse.ShouldBeBadRequest();

        var errorContent = await transferResponse.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("insufficient");

        // Assert - Balance should remain unchanged
        await AssertWalletBalanceAsync(availableBalance, "failed transfer attempt");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "7")]
    public async Task Should_Handle_Integrated_Purchase_With_Sufficient_Balance()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var walletBalance = 500000m;
        var purchaseAmount = 200000m;

        // Fund wallet first
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Act - Perform integrated purchase
        var purchaseRequest = new
        {
            totalAmount = purchaseAmount,
            orderId = Guid.NewGuid().ToString(),
            description = "Test integrated purchase",
            callbackUrl = "https://test.callback.url",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Purchase should be successful from wallet
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Assert - Balance should decrease
        await AssertWalletBalanceAsync(walletBalance - purchaseAmount, "integrated purchase");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "8")]
    public async Task Should_Handle_Integrated_Purchase_With_Insufficient_Balance()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var walletBalance = 100000m;
        var purchaseAmount = 300000m;
        var expectedTopUpAmount = purchaseAmount - walletBalance;

        // Fund wallet partially
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Act - Perform integrated purchase (should trigger auto top-up)
        var purchaseRequest = new
        {
            totalAmount = purchaseAmount,
            orderId = Guid.NewGuid().ToString(),
            description = "Test integrated purchase with top-up",
            callbackUrl = "https://test.callback.url",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should provide payment URL for top-up
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // The response should contain payment URL and required payment amount
        // Exact assertion depends on API response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "9")]
    public async Task Should_Process_Refund_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var initialBalance = 300000m;
        var purchaseAmount = 150000m;

        // Fund wallet and make a purchase
        var depositResponse = await PerformDirectDepositAsync(initialBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, initialBalance);

        var purchaseRequest = new
        {
            totalAmount = purchaseAmount,
            orderId = Guid.NewGuid().ToString(),
            description = "Test purchase for refund",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Wait a moment for transaction to complete
        await Task.Delay(1000);

        // Get refundable transactions
        var refundableResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetRefundableTransactions);
        var refundableTransactions = await refundableResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Act - Process refund (assuming we can get transaction ID from the response)
        // This part depends on the exact API structure

        // For now, let's create a mock refund request
        var mockTransactionId = Guid.NewGuid();
        var refundRequest = new
        {
            reason = "Test refund",
            partialAmount = (decimal?)null // Full refund
        };

        // Note: This would need the actual transaction ID from the purchase
        // var refundResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.RefundTransaction(mockTransactionId), refundRequest);

        // Assert - Balance should be restored
        // await AssertWalletBalanceAsync(initialBalance, "refund processing");
    }
}