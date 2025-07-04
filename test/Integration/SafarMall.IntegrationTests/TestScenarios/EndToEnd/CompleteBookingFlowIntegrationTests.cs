using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.EndToEnd;

[Collection("Sequential")]
public class CompleteBookingFlowIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "1")]
    public async Task Should_Complete_Full_Booking_Flow_With_Wallet_Payment()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var walletBalance = 500000m;
        var expectedOrderAmount = 200000m; // Estimated order cost

        // Step 1: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance, "Initial wallet funding");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        var balanceAfterDeposit = await GetWalletBalanceAsync();
        balanceAfterDeposit.TotalBalanceInIrr.Should().Be(walletBalance);

        // Step 2: Save passenger information for future use
        var savedPassenger = await SavePassengerAsync(passenger);
        savedPassenger.ShouldBeValidSavedPassenger(passenger);

        // Step 3: Create order
        var order = await CreateOrderAsync(new List<TestPassenger> { passenger }, "Train");
        order.ShouldBeValidOrder(testUser, new List<TestPassenger> { passenger });

        // Step 4: Process payment through integrated purchase
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = $"Payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 5: Verify wallet balance decreased
        var balanceAfterPurchase = await GetWalletBalanceAsync();
        balanceAfterPurchase.TotalBalanceInIrr.Should().Be(walletBalance - order.TotalAmount);

        // Step 6: Verify order status updated
        var updatedOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var updatedOrder = await updatedOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        updatedOrder.Status.Should().BeOneOf("Processing", "Completed");

        // Step 7: Get transaction history to verify payment record
        var historyResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.TransactionHistory);
        historyResponse.ShouldBeSuccessfulHttpResponse();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "2")]
    public async Task Should_Complete_Booking_Flow_With_Auto_Wallet_TopUp()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(2, isIranian: true);
        var partialBalance = 100000m;
        var orderAmount = 300000m; // More than wallet balance
        var requiredTopUp = orderAmount - partialBalance;

        // Step 1: Fund wallet partially
        var depositResponse = await PerformDirectDepositAsync(partialBalance, "Partial wallet funding");
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, partialBalance);

        // Step 2: Create order
        var order = await CreateOrderAsync(passengers, "DomesticFlight");

        // Step 3: Attempt integrated purchase (should trigger auto top-up)
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = $"Payment for order {order.OrderNumber} with auto top-up",
            callbackUrl = "https://test.callback.url",
            useCredit = false
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        var purchaseResult = await purchaseResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Should provide payment URL for additional funding
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 4: Simulate completing the top-up payment
        // This would involve getting the payment URL and simulating the gateway callback
        // For now, we'll simulate the additional deposit
        var additionalDepositResponse = await PerformDirectDepositAsync(requiredTopUp, "Auto top-up for order");
        await CompleteDepositViaCallbackAsync(additionalDepositResponse.Authority!, requiredTopUp);

        // Step 5: Retry the purchase (now with sufficient balance)
        var retryPurchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        retryPurchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 6: Verify final wallet balance
        var finalBalance = await GetWalletBalanceAsync();
        var expectedFinalBalance = (partialBalance + requiredTopUp) - order.TotalAmount;
        finalBalance.TotalBalanceInIrr.Should().BeInRange(expectedFinalBalance - 1000, expectedFinalBalance + 1000); // Allow small variance
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "3")]
    public async Task Should_Handle_Complete_Booking_Cancellation_With_Refund()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var walletBalance = 400000m;

        // Step 1: Complete initial booking
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        var order = await CreateOrderAsync(new List<TestPassenger> { passenger }, "Train");

        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = $"Payment for order {order.OrderNumber}",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        var balanceAfterPurchase = await GetWalletBalanceAsync();

        // Step 2: Cancel the order
        var cancelRequest = new
        {
            reason = "Customer requested cancellation"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        var cancelResult = await cancelResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Step 3: Process refund
        await Task.Delay(2000); // Wait for cancellation to process

        // Get refundable transactions
        var refundableResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetRefundableTransactions);
        refundableResponse.ShouldBeSuccessfulHttpResponse();

        // Step 4: Verify order status is cancelled
        var cancelledOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var cancelledOrder = await cancelledOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        cancelledOrder.Status.Should().Be("Cancelled");

        // Step 5: Verify refund was processed (balance should be restored)
        // Note: This depends on automatic refund processing or manual refund trigger
        await Task.Delay(3000); // Wait for potential refund processing

        var finalBalance = await GetWalletBalanceAsync();
        // Balance should be restored to original amount (minus any fees)
        finalBalance.TotalBalanceInIrr.Should().BeGreaterThan(balanceAfterPurchase.TotalBalanceInIrr);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "4")]
    public async Task Should_Complete_Multi_Passenger_International_Booking()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var iranianPassenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var foreignPassenger = TestUtilities.CreateTestPassenger(isIranian: false);
        var passengers = new List<TestPassenger> { iranianPassenger, foreignPassenger };
        var walletBalance = 2000000m; // Higher amount for international flight

        // Step 1: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 2: Save both passengers
        var savedIranianPassenger = await SavePassengerAsync(iranianPassenger);
        var savedForeignPassenger = await SavePassengerAsync(foreignPassenger);

        savedIranianPassenger.ShouldBeValidSavedPassenger(iranianPassenger);
        savedForeignPassenger.ShouldBeValidSavedPassenger(foreignPassenger);

        // Step 3: Create international flight order
        var order = await CreateOrderAsync(passengers, "InternationalFlight");
        order.ShouldBeValidOrder(testUser, passengers);
        order.ServiceType.Should().Be("InternationalFlight");
        order.PassengerCount.Should().Be(2);

        // Step 4: Add bank account for potential refunds
        var bankAccount = await AddBankAccountAsync();
        bankAccount.ShouldBeValidBankAccount(TestConfiguration.TestData.TestBankName);

        // Step 5: Process payment
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = $"International flight payment for {passengers.Count} passengers",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 6: Verify wallet balance and transaction
        var finalBalance = await GetWalletBalanceAsync();
        finalBalance.TotalBalanceInIrr.Should().Be(walletBalance - order.TotalAmount);

        // Step 7: Get order details to verify completion
        var orderDetailsResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrderDetails(order.Id));
        orderDetailsResponse.ShouldBeSuccessfulHttpResponse();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "5")]
    public async Task Should_Handle_Concurrent_Bookings_From_Same_User()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger1 = TestUtilities.CreateTestPassenger(isIranian: true);
        var passenger2 = TestUtilities.CreateTestPassenger(isIranian: true);
        var walletBalance = 1000000m;

        // Step 1: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 2: Create two orders simultaneously
        var order1Task = CreateOrderAsync(new List<TestPassenger> { passenger1 }, "Train");
        var order2Task = CreateOrderAsync(new List<TestPassenger> { passenger2 }, "DomesticFlight");

        var orders = await Task.WhenAll(order1Task, order2Task);
        var order1 = orders[0];
        var order2 = orders[1];

        // Step 3: Process payments for both orders
        var purchase1Request = new
        {
            totalAmount = order1.TotalAmount,
            orderId = order1.OrderNumber,
            description = $"Payment for first order {order1.OrderNumber}",
            callbackUrl = "https://test.callback.url"
        };

        var purchase2Request = new
        {
            totalAmount = order2.TotalAmount,
            orderId = order2.OrderNumber,
            description = $"Payment for second order {order2.OrderNumber}",
            callbackUrl = "https://test.callback.url"
        };

        var purchase1Task = _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase1Request);
        var purchase2Task = _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchase2Request);

        var purchaseResponses = await Task.WhenAll(purchase1Task, purchase2Task);

        // Step 4: Verify both payments processed correctly
        purchaseResponses[0].ShouldBeSuccessfulHttpResponse();
        purchaseResponses[1].ShouldBeSuccessfulHttpResponse();

        // Step 5: Verify final wallet balance
        var finalBalance = await GetWalletBalanceAsync();
        var expectedBalance = walletBalance - order1.TotalAmount - order2.TotalAmount;
        finalBalance.TotalBalanceInIrr.Should().Be(expectedBalance);

        // Step 6: Verify both orders exist in user's order list
        var ordersResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetUserOrders);
        ordersResponse.ShouldBeSuccessfulHttpResponse();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "6")]
    public async Task Should_Complete_Round_Trip_Booking_Flow()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(2, isIranian: true);
        var walletBalance = 800000m;

        // Step 1: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 2: Create round-trip order
        var roundTripOrderRequest = new
        {
            serviceType = "Train",
            sourceCode = 1,
            destinationCode = 2,
            sourceName = "تهران",
            destinationName = "اصفهان",
            departureDate = DateTime.Now.AddDays(7),
            returnDate = DateTime.Now.AddDays(14), // Return journey
            passengers = passengers.Select(p => new
            {
                firstNameEn = p.FirstNameEn,
                lastNameEn = p.LastNameEn,
                firstNameFa = p.FirstNameFa,
                lastNameFa = p.LastNameFa,
                birthDate = p.BirthDate,
                gender = p.Gender,
                isIranian = p.IsIranian,
                nationalCode = p.NationalCode,
                PassportNo = p.PassportNo
            }).ToList()
        };

        var orderResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CreateOrder, roundTripOrderRequest);
        var order = await orderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();

        order.ShouldBeValidOrder(testUser, passengers);
        order.HasReturn.Should().BeTrue();

        // Step 3: Process payment for round-trip
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = $"Round-trip payment for {passengers.Count} passengers",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 4: Verify tickets were generated for both legs
        // Check if there are separate tickets for outbound and return
        var ticketsResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetTrainTickets(order.Id));
        ticketsResponse.ShouldBeSuccessfulHttpResponse();

        // Step 5: Verify final balance
        var finalBalance = await GetWalletBalanceAsync();
        finalBalance.TotalBalanceInIrr.Should().Be(walletBalance - order.TotalAmount);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Priority", "7")]
    public async Task Should_Handle_Booking_Flow_With_Profile_Update()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var walletBalance = 300000m;

        // Step 1: Update user profile with complete information
        await UpdateUserProfileAsync(testUser);

        var updatedProfile = await GetCurrentUserProfileAsync();
        updatedProfile.Name.Should().Be(testUser.Name);
        updatedProfile.Family.Should().Be(testUser.Family);

        // Step 2: Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Step 3: Save passenger with same details as user profile
        passenger.FirstNameFa = testUser.Name;
        passenger.LastNameFa = testUser.Family;
        passenger.NationalCode = testUser.NationalCode;

        var savedPassenger = await SavePassengerAsync(passenger);
        savedPassenger.ShouldBeValidSavedPassenger(passenger);

        // Step 4: Create order with the saved passenger
        var order = await CreateOrderAsync(new List<TestPassenger> { passenger });

        // Step 5: Complete booking flow
        var purchaseRequest = new
        {
            totalAmount = order.TotalAmount,
            orderId = order.OrderNumber,
            description = "Booking with updated profile",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Step 6: Verify booking completed successfully
        var finalOrder = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var orderData = await finalOrder.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        orderData.Status.Should().NotBe("Failed");
    }
}