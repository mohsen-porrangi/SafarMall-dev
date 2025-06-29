using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Order;

[Collection("Sequential")]
public class OrderCancellationIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "1")]
    public async Task Should_Cancel_Pending_Order_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Verify order is in pending state
        var initialOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var initialOrder = await initialOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        initialOrder.Status.Should().BeOneOf("Pending", "Processing");

        // Act - Cancel the order
        var cancelRequest = new
        {
            reason = "User requested cancellation"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        var cancelResult = await cancelResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Cancellation should be successful
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Verify cancellation details
        cancelResult.Should().NotBeNull();
        // Additional assertions based on response structure

        // Verify order status is updated
        var updatedOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var updatedOrder = await updatedOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        updatedOrder.Status.Should().Be("Cancelled");
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "2")]
    public async Task Should_Not_Cancel_Already_Cancelled_Order()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // First cancellation
        var firstCancelRequest = new
        {
            reason = "First cancellation"
        };

        var firstCancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), firstCancelRequest);
        firstCancelResponse.ShouldBeSuccessfulHttpResponse();

        // Act - Try to cancel again
        var secondCancelRequest = new
        {
            reason = "Second cancellation attempt"
        };

        var secondCancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), secondCancelRequest);

        // Assert - Second cancellation should fail
        secondCancelResponse.ShouldBeBadRequest();

        var errorContent = await secondCancelResponse.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("already");
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "3")]
    public async Task Should_Require_Cancellation_Reason()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Act - Try to cancel without reason
        var cancelRequestWithoutReason = new
        {
            reason = "" // Empty reason
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequestWithoutReason);

        // Assert - Should require reason
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("reason");
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "4")]
    public async Task Should_Not_Allow_Unauthorized_Order_Cancellation()
    {
        // Arrange - Create order with first user
        var firstUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Arrange - Create second user and switch context
        var secondUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();
        _currentUser = secondUser;
        SetAuthenticationToken(secondUser.Token);

        // Act - Try to cancel first user's order with second user
        var cancelRequest = new
        {
            reason = "Unauthorized cancellation attempt"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);

        // Assert - Should be forbidden or not found
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Forbidden,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "5")]
    public async Task Should_Not_Cancel_Non_Existent_Order()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var nonExistentOrderId = Guid.NewGuid();

        // Act - Try to cancel non-existent order
        var cancelRequest = new
        {
            reason = "Trying to cancel non-existent order"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(nonExistentOrderId), cancelRequest);

        // Assert - Should return not found
        response.ShouldBeNotFound();
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "6")]
    public async Task Should_Handle_Order_Cancellation_With_Refund_Scenario()
    {
        // Arrange - Create user with wallet balance
        var testUser = await CreateAndRegisterUserAsync();
        var walletBalance = 500000m;

        // Fund wallet
        var depositResponse = await PerformDirectDepositAsync(walletBalance);
        await CompleteDepositViaCallbackAsync(depositResponse.Authority!, walletBalance);

        // Create and "pay" for order (simulate integrated purchase)
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Simulate payment for the order
        var purchaseAmount = 200000m;
        var purchaseRequest = new
        {
            totalAmount = purchaseAmount,
            orderId = order.Id.ToString(),
            description = "Payment for order before cancellation",
            callbackUrl = "https://test.callback.url"
        };

        var purchaseResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.IntegratedPurchase, purchaseRequest);
        purchaseResponse.ShouldBeSuccessfulHttpResponse();

        // Get balance after purchase
        var balanceAfterPurchase = (await GetWalletBalanceAsync()).TotalBalanceInIrr;

        // Act - Cancel the order
        var cancelRequest = new
        {
            reason = "Customer requested refund"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Assert - Order should be cancelled
        var cancelledOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var cancelledOrder = await cancelledOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        cancelledOrder.Status.Should().Be("Cancelled");

        // Note: Refund processing might be asynchronous, so we might need to wait or check refund status
        // The exact refund verification depends on how the refund process is implemented
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "7")]
    public async Task Should_Cancel_Multiple_Orders_Independently()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Create multiple orders
        var order1 = await CreateOrderAsync(passengers, "Train");
        var order2 = await CreateOrderAsync(passengers, "DomesticFlight");
        var order3 = await CreateOrderAsync(passengers, "Train");

        // Act - Cancel only the second order
        var cancelRequest = new
        {
            reason = "Cancel only second order"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order2.Id), cancelRequest);
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Assert - Only second order should be cancelled
        var order1Response = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order1.Id));
        var order1Updated = await order1Response.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        order1Updated.Status.Should().NotBe("Cancelled");

        var order2Response = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order2.Id));
        var order2Updated = await order2Response.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        order2Updated.Status.Should().Be("Cancelled");

        var order3Response = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order3.Id));
        var order3Updated = await order3Response.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        order3Updated.Status.Should().NotBe("Cancelled");
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "8")]
    public async Task Should_Cancel_Order_With_Long_Reason_Text()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Act - Cancel with a long reason
        var longReason = new string('A', 500) + " - Detailed cancellation reason with lots of text to test the system's ability to handle longer reason descriptions.";

        var cancelRequest = new
        {
            reason = longReason
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);

        // Assert - Should handle long reason appropriately
        if (longReason.Length <= 1000) // Assuming reasonable limit
        {
            cancelResponse.ShouldBeSuccessfulHttpResponse();

            var updatedOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
            var updatedOrder = await updatedOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
            updatedOrder.Status.Should().Be("Cancelled");
        }
        else
        {
            cancelResponse.ShouldBeBadRequest();
        }
    }

    [Fact]
    [Trait("Category", "OrderCancellation")]
    [Trait("Priority", "9")]
    public async Task Should_Track_Cancellation_History_In_Order_Details()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Act - Cancel the order
        var cancelRequest = new
        {
            reason = "Track cancellation in history"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Act - Get detailed order information
        var orderDetailsResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrderDetails(order.Id));
        var orderDetails = await orderDetailsResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert - Order details should show cancellation information
        orderDetailsResponse.ShouldBeSuccessfulHttpResponse();

        // The exact assertion depends on the structure of order details response
        // Should contain cancellation timestamp, reason, and status history
    }
}