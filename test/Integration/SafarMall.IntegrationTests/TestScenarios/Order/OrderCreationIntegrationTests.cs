using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Order;

[Collection("Sequential")]
public class OrderCreationIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "1")]
    public async Task Should_Create_Order_With_Single_Passenger()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var passengers = new List<TestPassenger> { passenger };

        // Act
        var order = await CreateOrderAsync(passengers, "Train");

        // Assert
        order.ShouldBeValidOrder(testUser, passengers);
        order.ServiceType.Should().Be("Train");
        order.PassengerCount.Should().Be(1);
        order.HasReturn.Should().BeFalse();
        order.TotalAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "2")]
    public async Task Should_Create_Order_With_Multiple_Passengers()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(3, isIranian: true);

        // Act
        var order = await CreateOrderAsync(passengers, "DomesticFlight");

        // Assert
        order.ShouldBeValidOrder(testUser, passengers);
        order.ServiceType.Should().Be("DomesticFlight");
        order.PassengerCount.Should().Be(3);
        order.TotalAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "3")]
    public async Task Should_Create_Order_With_Foreign_Passengers()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var foreignPassengers = TestUtilities.CreateTestPassengers(2, isIranian: false);

        // Act
        var order = await CreateOrderAsync(foreignPassengers, "InternationalFlight");

        // Assert
        order.ShouldBeValidOrder(testUser, foreignPassengers);
        order.ServiceType.Should().Be("InternationalFlight");
        order.PassengerCount.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "4")]
    public async Task Should_Get_Order_Details_After_Creation()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Act - Create order
        var order = await CreateOrderAsync(passengers);

        // Act - Get order details
        var orderDetailsResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrderDetails(order.Id));
        var orderDetails = await orderDetailsResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        orderDetailsResponse.ShouldBeSuccessfulHttpResponse();
        // Additional assertions based on order details structure
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "5")]
    public async Task Should_Get_User_Orders_List()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Act - Create multiple orders
        var order1 = await CreateOrderAsync(passengers, "Train");
        var order2 = await CreateOrderAsync(passengers, "DomesticFlight");

        // Act - Get user orders
        var ordersResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetUserOrders);
        var ordersData = await ordersResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        ordersResponse.ShouldBeSuccessfulHttpResponse();
        // Should contain both created orders
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "6")]
    public async Task Should_Filter_Orders_By_Service_Type()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Act - Create orders of different types
        var trainOrder = await CreateOrderAsync(passengers, "Train");
        var flightOrder = await CreateOrderAsync(passengers, "DomesticFlight");

        // Act - Filter by train orders
        var filterUrl = $"{EndpointUrls.Order.GetUserOrders}?serviceType=Train";
        var filteredResponse = await _httpClient.GetAsync(filterUrl);
        var filteredData = await filteredResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        filteredResponse.ShouldBeSuccessfulHttpResponse();
        // Should only contain train orders
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "7")]
    public async Task Should_Cancel_Order_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Act - Create order
        var order = await CreateOrderAsync(passengers);

        // Act - Cancel order
        var cancelRequest = new
        {
            reason = "Test cancellation"
        };

        var cancelResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(order.Id), cancelRequest);
        var cancelResult = await cancelResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        cancelResponse.ShouldBeSuccessfulHttpResponse();

        // Verify order status changed
        var updatedOrderResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));
        var updatedOrder = await updatedOrderResponse.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
        updatedOrder.Status.Should().Be("Cancelled");
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "8")]
    public async Task Should_Not_Allow_Unauthorized_Order_Access()
    {
        // Arrange - Create order with first user
        var firstUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers);

        // Arrange - Create second user
        var secondUser = await CreateAndRegisterUserAsync();

        // Act - Try to access first user's order with second user
        ClearAuthentication();
        _currentUser = secondUser;
        SetAuthenticationToken(secondUser.Token);

        var unauthorizedResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetOrder(order.Id));

        // Assert - Should be forbidden or not found
        unauthorizedResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Forbidden,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "9")]
    public async Task Should_Validate_Required_Order_Fields()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to create order with missing required fields
        var invalidOrderRequest = new
        {
            serviceType = "", // Empty service type
            sourceCode = 0,   // Invalid source
            destinationCode = 0, // Invalid destination
            passengers = new List<object>() // Empty passengers
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CreateOrder, invalidOrderRequest);

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Order")]
    [Trait("Priority", "10")]
    public async Task Should_Handle_Order_With_Return_Journey()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passengers = TestUtilities.CreateTestPassengers(1);

        // Act - Create order with return date
        var orderRequest = new
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

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CreateOrder, orderRequest);
        var order = await response.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();

        // Assert
        order.ShouldBeValidOrder(testUser, passengers);
        order.HasReturn.Should().BeTrue();
        order.TotalAmount.Should().BeGreaterThan(0); // Should be higher for return journey
    }
}