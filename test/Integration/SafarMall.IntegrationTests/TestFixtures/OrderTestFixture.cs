using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;

namespace SafarMall.IntegrationTests.TestFixtures;

/// <summary>
/// Test fixture for Order service integration tests
/// Provides common setup and helper methods for order-related testing
/// </summary>
public class OrderTestFixture : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly List<TestUser> _createdUsers = new();
    private readonly List<Guid> _createdOrders = new();
    private readonly List<long> _createdPassengers = new();

    public OrderTestFixture()
    {
        _httpClient = CreateHttpClient();
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "SafarMall-OrderTests/1.0");
        client.Timeout = TestConfiguration.Timeouts.DefaultTimeout;
        return client;
    }

    #region Order Creation Helpers

    /// <summary>
    /// Create a complete test order with user, wallet funding, and passengers
    /// </summary>
    public async Task<(TestUser User, OrderResponse Order, List<TestPassenger> Passengers)> CreateCompleteTestOrderAsync(
        string serviceType = "Train",
        int passengerCount = 1,
        bool isIranian = true,
        decimal walletBalance = 500000m)
    {
        // Create and register user
        var user = await CreateTestUserWithWalletAsync(walletBalance);

        // Create passengers
        var passengers = TestUtilities.CreateTestPassengers(passengerCount, isIranian);

        // Create order
        var order = await CreateOrderAsync(user, passengers, serviceType);

        _createdOrders.Add(order.Id);

        return (user, order, passengers);
    }

    /// <summary>
    /// Create test user with funded wallet
    /// </summary>
    public async Task<TestUser> CreateTestUserWithWalletAsync(decimal initialBalance = 0)
    {
        var user = TestUtilities.CreateTestUser();

        // Register user
        await RegisterUserAsync(user);
        await VerifyUserOtpAsync(user);

        _createdUsers.Add(user);

        // Fund wallet if requested
        if (initialBalance > 0)
        {
            await FundUserWalletAsync(user, initialBalance);
        }

        return user;
    }

    /// <summary>
    /// Create order for specific user
    /// </summary>
    public async Task<OrderResponse> CreateOrderAsync(
        TestUser user,
        List<TestPassenger> passengers,
        string serviceType = "Train",
        bool hasReturn = false)
    {
        _httpClient.AddAuthorizationHeader(user.Token);

        var orderRequest = new
        {
            serviceType = serviceType,
            sourceCode = GetSourceCodeForServiceType(serviceType),
            destinationCode = GetDestinationCodeForServiceType(serviceType),
            sourceName = GetSourceNameForServiceType(serviceType),
            destinationName = GetDestinationNameForServiceType(serviceType),
            departureDate = DateTime.Now.AddDays(7),
            returnDate = hasReturn ? DateTime.Now.AddDays(14) : (DateTime?)null,
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

        _createdOrders.Add(order.Id);
        return order;
    }

    #endregion

    #region Passenger Management Helpers

    /// <summary>
    /// Save multiple passengers for a user
    /// </summary>
    public async Task<List<SavedPassengerResponse>> SavePassengersAsync(TestUser user, List<TestPassenger> passengers)
    {
        _httpClient.AddAuthorizationHeader(user.Token);
        var savedPassengers = new List<SavedPassengerResponse>();

        foreach (var passenger in passengers)
        {
            var savedPassenger = await SaveSinglePassengerAsync(passenger);
            savedPassengers.Add(savedPassenger);
            _createdPassengers.Add(savedPassenger.Id);
        }

        return savedPassengers;
    }

    /// <summary>
    /// Save single passenger
    /// </summary>
    public async Task<SavedPassengerResponse> SaveSinglePassengerAsync(TestPassenger passenger)
    {
        var passengerRequest = new
        {
            firstNameEn = passenger.FirstNameEn,
            lastNameEn = passenger.LastNameEn,
            firstNameFa = passenger.FirstNameFa,
            lastNameFa = passenger.LastNameFa,
            nationalCode = passenger.NationalCode,
            PassportNo = passenger.PassportNo,
            birthDate = passenger.BirthDate,
            gender = passenger.Gender
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.SavePassenger, passengerRequest);
        var savedPassenger = await response.EnsureSuccessAndReadAsJsonAsync<SavedPassengerResponse>();

        _createdPassengers.Add(savedPassenger.Id);
        return savedPassenger;
    }

    #endregion

    #region Order Status Management

    /// <summary>
    /// Cancel order
    /// </summary>
    public async Task<dynamic> CancelOrderAsync(TestUser user, Guid orderId, string reason = "Test cancellation")
    {
        _httpClient.AddAuthorizationHeader(user.Token);

        var cancelRequest = new { reason = reason };
        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.CancelOrder(orderId), cancelRequest);

        return await response.EnsureSuccessAndReadAsJsonAsync<dynamic>();
    }

    /// <summary>
    /// Get order details
    /// </summary>
    public async Task<dynamic> GetOrderDetailsAsync(TestUser user, Guid orderId)
    {
        _httpClient.AddAuthorizationHeader(user.Token);

        var response = await _httpClient.GetAsync(EndpointUrls.Order.GetOrderDetails(orderId));
        return await response.EnsureSuccessAndReadAsJsonAsync<dynamic>();
    }

    /// <summary>
    /// Get user orders with filtering
    /// </summary>
    public async Task<dynamic> GetUserOrdersAsync(
        TestUser user,
        string? serviceType = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        _httpClient.AddAuthorizationHeader(user.Token);

        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(serviceType))
            queryParams.Add($"serviceType={serviceType}");

        if (!string.IsNullOrEmpty(status))
            queryParams.Add($"status={status}");

        var queryString = string.Join("&", queryParams);
        var url = $"{EndpointUrls.Order.GetUserOrders}?{queryString}";

        var response = await _httpClient.GetAsync(url);
        return await response.EnsureSuccessAndReadAsJsonAsync<dynamic>();
    }

    #endregion

    #region Service Type Helpers

    private static int GetSourceCodeForServiceType(string serviceType)
    {
        return serviceType switch
        {
            "Train" => 1, // Tehran
            "DomesticFlight" => 10, // Tehran Airport
            "InternationalFlight" => 10, // Tehran Airport
            _ => 1
        };
    }

    private static int GetDestinationCodeForServiceType(string serviceType)
    {
        return serviceType switch
        {
            "Train" => 2, // Isfahan
            "DomesticFlight" => 20, // Isfahan Airport
            "InternationalFlight" => 100, // Dubai Airport
            _ => 2
        };
    }

    private static string GetSourceNameForServiceType(string serviceType)
    {
        return serviceType switch
        {
            "Train" => "تهران",
            "DomesticFlight" => "فرودگاه تهران",
            "InternationalFlight" => "فرودگاه تهران",
            _ => "تهران"
        };
    }

    private static string GetDestinationNameForServiceType(string serviceType)
    {
        return serviceType switch
        {
            "Train" => "اصفهان",
            "DomesticFlight" => "فرودگاه اصفهان",
            "InternationalFlight" => "فرودگاه دبی",
            _ => "اصفهان"
        };
    }

    #endregion

    #region User and Wallet Helpers

    private async Task RegisterUserAsync(TestUser user)
    {
        var registerRequest = new
        {
            mobile = user.Mobile,
            password = user.Password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Register, registerRequest);
        response.EnsureSuccessStatusCode();
    }

    private async Task VerifyUserOtpAsync(TestUser user)
    {
        var verifyRequest = new
        {
            mobile = user.Mobile,
            otp = TestConfiguration.TestData.DefaultOtp
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.VerifyOtp, verifyRequest);
        var loginResult = await response.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        user.Token = loginResult.Token!;

        // Get user profile to set ID
        _httpClient.AddAuthorizationHeader(user.Token);
        var profileResponse = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);
        var profile = await profileResponse.EnsureSuccessAndReadAsJsonAsync<UserProfileResponse>();
        user.Id = profile.Id;
    }

    private async Task FundUserWalletAsync(TestUser user, decimal amount)
    {
        _httpClient.AddAuthorizationHeader(user.Token);

        // Perform direct deposit
        var depositRequest = new
        {
            amount = amount,
            description = "Test wallet funding",
            callbackUrl = "https://test.callback.url"
        };

        var depositResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.DirectDeposit, depositRequest);
        var depositResult = await depositResponse.EnsureSuccessAndReadAsJsonAsync<DirectDepositResponse>();

        // Complete deposit via callback
        var callbackRequest = new
        {
            authority = depositResult.Authority,
            status = "OK",
            amount = amount
        };

        await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, callbackRequest);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup created test data
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            // Cancel all created orders
            foreach (var userId in _createdUsers.Select(u => u.Id))
            {
                var user = _createdUsers.First(u => u.Id == userId);
                _httpClient.AddAuthorizationHeader(user.Token);

                foreach (var orderId in _createdOrders)
                {
                    try
                    {
                        await CancelOrderAsync(user, orderId, "Test cleanup");
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            // Delete saved passengers
            foreach (var userId in _createdUsers.Select(u => u.Id))
            {
                var user = _createdUsers.First(u => u.Id == userId);
                _httpClient.AddAuthorizationHeader(user.Token);

                foreach (var passengerId in _createdPassengers)
                {
                    try
                    {
                        await _httpClient.DeleteAsync(EndpointUrls.Order.DeleteSavedPassenger(passengerId));
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public void Dispose()
    {
        try
        {
            CleanupAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _httpClient?.Dispose();
        }
    }

    #endregion
}