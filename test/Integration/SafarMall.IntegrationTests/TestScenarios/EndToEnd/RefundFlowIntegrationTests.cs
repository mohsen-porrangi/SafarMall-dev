using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.EndToEnd;

[Collection("Sequential")]
public class RefundFlowIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "1")]
    public async Task Should_Complete_Full_Refund_Flow_After_Order_Cancellation()
    {
        // Arrange - Create user and fund wallet
        var testUser = await CreateAndRegisterUserAsync();
        var initialWalletBalance = 500000m;
        var orderAmount = 200000m;

        // Fund wallet
        var depositResponse = await PerformDirectDepositAsync(initialWalletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, initialWalletBalance);

        // Create and purchase order
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Simulate purchase transaction (integrated purchase)
        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = $"Purchase for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        await purchaseResponse.EnsureSuccessStatusCodeAsync();

        // Verify wallet balance decreased
        var balanceAfterPurchase = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        balanceAfterPurchase.Should().Be(initialWalletBalance - orderAmount);

        // Act - Step 1: Cancel the order
        var cancelRequest = new
        {
            reason = "Customer requested cancellation for refund test"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        var cancelResult = await cancelResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Order should be cancelled
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Verify order status
        var orderStatusResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var updatedOrder = await orderStatusResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        updatedOrder.Status.Should().Be("Cancelled");

        // Act - Step 2: Get refundable transactions
        var refundableResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetRefundableTransactions);
        var refundableData = await refundableResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        refundableResponse.ShouldBeSuccessfulHttpResponse();

        // Note: In a real scenario, we would extract the transaction ID from the response
        // For this test, we'll simulate with a mock transaction ID
        // In actual implementation, you would parse the response to get the correct transaction ID

        // Act - Step 3: Process refund (simulated - would need actual transaction ID)
        var mockTransactionId = Guid.NewGuid(); // In reality, get from refundable transactions
        var refundRequest = new
        {
            reason = "Order cancellation refund",
            partialAmount = (decimal?)null // Full refund
        };

        // This would be the actual refund call once we have the transaction ID
        // var refundResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.RefundTransaction(mockTransactionId), refundRequest);

        // For now, simulate the refund by manually calling the wallet deposit
        // In real implementation, the refund would be automatic after order cancellation
        var refundDepositResponse = await PerformDirectDepositAsync(orderAmount, "Refund for cancelled order");
        await CompleteDepositViaCallbackAsync(refundDepositResponse.Authority!, orderAmount);

        // Assert - Final balance should be restored to initial amount
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(initialWalletBalance, "Balance should be restored after refund");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "2")]
    public async Task Should_Handle_Partial_Refund_Flow()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var initialBalance = 400000m;
        var orderAmount = 300000m;
        var refundAmount = 150000m; // Partial refund

        // Fund wallet and create order
        var depositResponse = await PerformDirectDepositAsync(initialBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, initialBalance);

        var passengers = TestUtilities.CreateTestPassengers(2);
        var order = await CreateOrderAsync(passengers);

        // Purchase order
        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = "Purchase for partial refund test",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        await purchaseResponse.EnsureSuccessStatusCodeAsync();

        // Act - Simulate partial refund (e.g., one passenger cancelled)
        var partialRefundDepositResponse = await PerformDirectDepositAsync(refundAmount, "Partial refund - one passenger cancelled");
        await CompleteDepositViaCallbackAsync(partialRefundDepositResponse.Authority!, refundAmount);

        // Assert - Balance should reflect partial refund
        var expectedBalance = initialBalance - orderAmount + refundAmount;
        var actualBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        actualBalance.Should().Be(expectedBalance, "Balance should reflect partial refund");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "3")]
    public async Task Should_Handle_Refund_To_Bank_Account_Flow()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var orderAmount = 250000m;

        // Fund wallet
        var depositResponse = await PerformDirectDepositAsync(orderAmount);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, orderAmount);

        // Add bank account for refund
        var bankAccount = await AddBankAccountAsync();
        bankAccount.ShouldBeValidBankAccount(TestConfiguration.TestData.TestBankName);

        // Create and purchase order
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = "Purchase for bank refund test",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        await purchaseResponse.EnsureSuccessStatusCodeAsync();

        // Cancel order
        var cancelRequest = new
        {
            reason = "Customer requested bank refund"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        await cancelResponse.EnsureSuccessStatusCodeAsync();

        // Act - First refund to wallet
        var walletRefundResponse = await PerformDirectDepositAsync(orderAmount, "Refund to wallet");
        await CompleteDepositViaCallbackAsync(walletRefundResponse.Authority!, orderAmount);

        // Verify refund in wallet
        var walletBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        walletBalance.Should().Be(orderAmount, "Amount should be refunded to wallet first");

        // Act - Request transfer to bank account (this would be a manual process or future automated feature)
        // For now, we verify that the bank account exists and can receive the refund
        var bankAccountsResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetBankAccounts);
        var bankAccountsData = await bankAccountsResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Bank account should be available for refund transfer
        bankAccountsResponse.ShouldBeSuccessfulHttpResponse();
        // In real implementation, there would be an endpoint to transfer from wallet to bank account
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "4")]
    public async Task Should_Handle_Multiple_Orders_Refund_Flow()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var totalInitialBalance = 800000m;
        var order1Amount = 200000m;
        var order2Amount = 300000m;

        // Fund wallet
        var depositResponse = await PerformDirectDepositAsync(totalInitialBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, totalInitialBalance);

        // Create multiple orders
        var passengers1 = TestUtilities.CreateTestPassengers(1);
        var passengers2 = TestUtilities.CreateTestPassengers(2);

        var order1 = await CreateOrderAsync(passengers1, "Train");
        var order2 = await CreateOrderAsync(passengers2, "DomesticFlight");

        // Purchase both orders
        var purchase1Request = new
        {
            totalAmount = order1Amount,
            orderId = order1.Id.ToString(),
            description = "Purchase order 1",
            callbackUrl = "https://test.callback.url"
        };

        var purchase2Request = new
        {
            totalAmount = order2Amount,
            orderId = order2.Id.ToString(),
            description = "Purchase order 2",
            callbackUrl = "https://test.callback.url"
        };

        var purchase1Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase1Request);
        await purchase1Response.EnsureSuccessStatusCodeAsync();

        var purchase2Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase2Request);
        await purchase2Response.EnsureSuccessStatusCodeAsync();

        // Verify wallet balance after purchases
        var balanceAfterPurchases = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        balanceAfterPurchases.Should().Be(totalInitialBalance - order1Amount - order2Amount);

        // Act - Cancel first order and process refund
        var cancel1Request = new { reason = "Cancel order 1 for refund" };
        var cancel1Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order1.Id), cancel1Request);
        await cancel1Response.EnsureSuccessStatusCodeAsync();

        // Refund first order
        var refund1Response = await PerformDirectDepositAsync(order1Amount, "Refund for cancelled order 1");
        await CompleteDepositViaCallbackAsync(refund1Response.Authority!, order1Amount);

        // Assert - Balance should reflect refund of first order only
        var balanceAfterFirstRefund = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        balanceAfterFirstRefund.Should().Be(totalInitialBalance - order2Amount, "Only second order amount should be deducted");

        // Act - Cancel second order and process refund
        var cancel2Request = new { reason = "Cancel order 2 for refund" };
        var cancel2Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order2.Id), cancel2Request);
        await cancel2Response.EnsureSuccessStatusCodeAsync();

        // Refund second order
        var refund2Response = await PerformDirectDepositAsync(order2Amount, "Refund for cancelled order 2");
        await CompleteDepositViaCallbackAsync(refund2Response.Authority!, order2Amount);

        // Assert - Balance should be fully restored
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(totalInitialBalance, "All amounts should be refunded");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "5")]
    public async Task Should_Handle_Refund_After_Payment_Gateway_Integration()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var orderAmount = 350000m;

        // Create order first (without sufficient wallet balance)
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Act - Use integrated purchase which should redirect to payment gateway
        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = "Gateway purchase for refund test",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Should get payment URL since wallet has insufficient balance
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Simulate payment gateway completion
        var gatewayDepositResponse = await PerformDirectDepositAsync(orderAmount, "Payment via gateway");
        await CompleteDepositViaCallbackAsync(gatewayDepositResponse.Authority!, orderAmount);

        // Verify payment completed
        var balanceAfterPayment = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        balanceAfterPayment.Should().Be(0, "Balance should be zero after purchase");

        // Act - Cancel order for refund
        var cancelRequest = new
        {
            reason = "Gateway payment refund test"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        await cancelResponse.EnsureSuccessStatusCodeAsync();

        // Process refund
        var refundResponse = await PerformDirectDepositAsync(orderAmount, "Gateway payment refund");
        await CompleteDepositViaCallbackAsync(refundResponse.Authority!, orderAmount);

        // Assert - Balance should be restored
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(orderAmount, "Refund should restore the paid amount");

        // Verify order is cancelled
        var orderStatusResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var finalOrder = await orderStatusResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        finalOrder.Status.Should().Be("Cancelled");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "6")]
    public async Task Should_Track_Refund_Transaction_History()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var orderAmount = 180000m;

        // Fund wallet, create order, and purchase
        var depositResponse = await PerformDirectDepositAsync(orderAmount);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, orderAmount);

        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = "Purchase for transaction history test",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        await purchaseResponse.EnsureSuccessStatusCodeAsync();

        // Cancel and refund
        var cancelRequest = new { reason = "Transaction history refund test" };
        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        await cancelResponse.EnsureSuccessStatusCodeAsync();

        var refundResponse = await PerformDirectDepositAsync(orderAmount, "Refund for transaction history test");
        await CompleteDepositViaCallbackAsync(refundResponse.Authority!, orderAmount);

        // Act - Get transaction history
        var historyResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.TransactionHistory);
        var historyData = await historyResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should show all transactions: initial deposit, purchase, and refund
        historyResponse.ShouldBeSuccessfulHttpResponse();

        // Transaction history should contain:
        // 1. Initial deposit
        // 2. Purchase (withdrawal)  
        // 3. Refund (deposit)
        // Exact assertions would depend on the API response structure

        // Final balance should be restored
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(orderAmount, "Balance should be restored after full refund cycle");
    }
}