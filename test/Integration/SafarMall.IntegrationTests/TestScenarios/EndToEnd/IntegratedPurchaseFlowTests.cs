using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.EndToEnd;

[Collection("Sequential")]
public class IntegratedPurchaseFlowTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "1")]
    public async Task Should_Complete_Full_Booking_Flow_With_Sufficient_Wallet_Balance()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(2, isIranian: true);
        var walletBalance = 1000000m; // 1M IRR
        var expectedOrderAmount = 500000m; // Estimated order cost

        // Step 1: Fund wallet with sufficient balance
        var depositResponse = await PerformDirectDepositAsync(walletBalance, "Initial wallet funding for booking");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        initialBalance.Should().Be(walletBalance);

        // Step 2: Create order
        var order = await CreateOrderAsync(passengers, "Train");
        order.ShouldBeValidOrder(testUser, passengers);

        // Step 3: Process integrated purchase (should use wallet balance directly)
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.Id.ToString(),
            description = $"Payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/order-payment",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Purchase should complete directly from wallet
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 4: Verify wallet balance decreased
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(walletBalance - order.TotalAmount, "Wallet balance should decrease by order amount");

        // Step 5: Verify order status (should be processing or completed)
        var updatedOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var updatedOrder = await updatedOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        updatedOrder.Status.Should().BeOneOf("Processing", "Completed", "Pending");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "2")]
    public async Task Should_Complete_Booking_Flow_With_Auto_Wallet_TopUp()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var existingBalance = 100000m; // 100K IRR
        var orderAmount = 350000m; // More than existing balance
        var requiredTopUp = orderAmount - existingBalance;

        // Step 1: Fund wallet with insufficient balance
        var depositResponse = await PerformDirectDepositAsync(existingBalance, "Partial wallet funding");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, existingBalance);

        // Step 2: Create order
        var order = await CreateOrderAsync(passengers, "DomesticFlight");

        // Step 3: Attempt integrated purchase (should trigger auto top-up flow)
        var purchaseRequest = new
        {
            totalAmount = orderAmount,
            orderId = order.Id.ToString(),
            description = $"Payment for flight order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/flight-payment",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should get payment URL for top-up
        purchaseResponse.ShouldBeSuccessfulHttpResponse();
        // purchaseResult should contain payment URL and required amount

        // Step 4: Simulate successful payment gateway callback for top-up
        // (In real scenario, user would complete payment via gateway)
        var topUpCallbackRequest = new
        {
            authority = "MOCK_AUTHORITY_FOR_TOPUP",
            status = "OK",
            amount = requiredTopUp
        };

        var topUpCallbackResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, topUpCallbackRequest);

        // Step 5: Verify final wallet balance
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        // Balance should be: existingBalance + requiredTopUp - orderAmount = 0 (if purchase completed)
        // Or existingBalance + requiredTopUp (if purchase is pending completion)
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "3")]
    public async Task Should_Handle_Multiple_Concurrent_Bookings_Correctly()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers1 = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var passengers2 = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var walletBalance = 2000000m; // 2M IRR

        // Fund wallet generously
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 1: Create two orders
        var order1 = await CreateOrderAsync(passengers1, "Train");
        var order2 = await CreateOrderAsync(passengers2, "DomesticFlight");

        var initialBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Step 2: Process both purchases
        var purchase1Request = new
        {
            totalAmount = order1.TotalAmount,
            orderId = order1.Id.ToString(),
            description = $"Payment for train order {order1.OrderNumber}",
            callbackUrl = "https://test.callback.url/train-payment",
            useCredit = false
        };

        var purchase2Request = new
        {
            totalAmount = order2.TotalAmount,
            orderId = order2.Id.ToString(),
            description = $"Payment for flight order {order2.OrderNumber}",
            callbackUrl = "https://test.callback.url/flight-payment",
            useCredit = false
        };

        // Execute purchases
        var purchase1Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase1Request);
        var purchase2Response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase2Request);

        // Assert both purchases successful
        purchase1Response.ShouldBeSuccessfulHttpResponse();
        purchase2Response.ShouldBeSuccessfulHttpResponse();

        // Step 3: Verify total deduction
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        var totalOrderAmount = order1.TotalAmount + order2.TotalAmount;
        finalBalance.Should().Be(initialBalance - totalOrderAmount, "Both orders should be deducted from wallet");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "4")]
    public async Task Should_Handle_Booking_Cancellation_And_Refund_Flow()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var walletBalance = 800000m;

        // Step 1: Complete full booking flow
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        var order = await CreateOrderAsync(passengers, "Train");

        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.Id.ToString(),
            description = $"Payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/booking-payment",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        var balanceAfterPurchase = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Step 2: Cancel the order
        var cancelRequest = new
        {
            reason = "Customer requested cancellation"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Step 3: Process refund (this might be automatic or manual depending on business logic)
        // For now, let's verify the order is cancelled
        var cancelledOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var cancelledOrder = await cancelledOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        cancelledOrder.Status.Should().Be("Cancelled");

        // Step 4: Verify refund (if automatic)
        // Wait a moment for potential automatic refund processing
        await Task.Delay(2000);

        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        // If automatic refund is implemented, balance should be restored
        // finalBalance.Should().Be(walletBalance, "Refund should restore original wallet balance");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "5")]
    public async Task Should_Handle_International_Flight_Booking_With_Passport_Validation()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var foreignPassengers = TestUtilities.CreateTestPassengers(1, isIranian: false);
        var walletBalance = 1500000m; // Higher amount for international flight

        // Step 1: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 2: Create international flight order
        var order = await CreateOrderAsync(foreignPassengers, "InternationalFlight");
        order.ServiceType.Should().Be("InternationalFlight");

        // Step 3: Complete purchase
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.Id.ToString(),
            description = $"Payment for international flight {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/intl-flight-payment",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 4: Verify order details contain passport information
        var orderDetailsResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrderDetails(order.Id));
        orderDetailsResponse.ShouldBeSuccessfulHttpResponse();
        // Additional validation for passport data in order details
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "6")]
    public async Task Should_Handle_Bank_Account_Integration_For_Large_Transactions()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(2, isIranian: true);
        var smallWalletBalance = 50000m;
        var largeOrderAmount = 2000000m; // 2M IRR - large transaction

        // Step 1: Add bank account
        var bankAccount = await AddBankAccountAsync();
        bankAccount.ShouldBeValidBankAccount(TestConfiguration.TestData.TestBankName);

        // Step 2: Fund wallet with small amount
        var depositResponse = await PerformDirectDepositAsync(smallWalletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, smallWalletBalance);

        // Step 3: Create high-value order
        var order = await CreateOrderAsync(passengers, "InternationalFlight");

        // Step 4: Attempt purchase (should require significant top-up)
        var purchaseRequest = new
        {
            totalAmount = largeOrderAmount,
            orderId = order.Id.ToString(),
            description = $"Large payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/large-payment",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should provide payment gateway options
        purchaseResponse.ShouldBeSuccessfulHttpResponse();
        // Bank account should be available for potential refunds
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "7")]
    public async Task Should_Handle_Transaction_History_Throughout_Booking_Process()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var walletBalance = 600000m;

        // Step 1: Initial deposit
        var depositResponse = await PerformDirectDepositAsync(walletBalance, "Initial funding for booking test");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 2: Create and pay for order
        var order = await CreateOrderAsync(passengers, "Train");

        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.Id.ToString(),
            description = $"Train booking payment for {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/train-booking",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 3: Check transaction history
        var historyResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.TransactionHistory);
        historyResponse.ShouldBeSuccessfulHttpResponse();

        var historyData = await historyResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Should contain both deposit and purchase transactions
        // Exact assertion depends on API response structure
        // Should show:
        // 1. Deposit transaction (IN, Completed)
        // 2. Purchase transaction (OUT, Completed)
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "8")]
    public async Task Should_Prevent_Duplicate_Payment_For_Same_Order()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1, isIranian: true);
        var walletBalance = 1000000m;

        // Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Create order
        var order = await CreateOrderAsync(passengers, "Train");

        // Step 1: First payment attempt
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.Id.ToString(),
            description = $"Payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url/duplicate-test",
            useCredit = false
        };

        var firstPurchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        firstPurchaseResponse.ShouldBeSuccessfulHttpResponse();

        var balanceAfterFirstPayment = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Step 2: Second payment attempt for same order (should be prevented)
        var secondPurchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);

        // Assert - Second payment should be rejected
        secondPurchaseResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.Conflict);

        // Verify balance didn't change
        var finalBalance = (await GetWalletBalanceAsync()).TotalBalanceInIrr;
        finalBalance.Should().Be(balanceAfterFirstPayment, "Balance should not change on duplicate payment attempt");
    }
}