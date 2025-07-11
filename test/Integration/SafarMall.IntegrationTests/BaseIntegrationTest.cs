using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using System.Net.Http.Headers;

namespace SafarMall.IntegrationTests;

public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly HttpClient _httpClient;
    protected readonly string _correlationId;
    protected TestUser? _currentUser;

    protected BaseIntegrationTest()
    {
        _httpClient = CreateHttpClient();
        _correlationId = Guid.NewGuid().ToString();
        _httpClient.AddCorrelationId(_correlationId);
        _httpClient.Timeout = TestConfiguration.Timeouts.DefaultTimeout;
    }

    protected virtual HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler()
        {
            // Trust all certificates for testing (development certificates)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            // Disable certificate revocation checks
            CheckCertificateRevocationList = false,
            // Use TLS 1.2 and 1.3
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
        var client = new HttpClient(handler);

        // Configure default headers
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("User-Agent", "SafarMall-IntegrationTests/1.0");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.Timeout = TestConfiguration.Timeouts.DefaultTimeout;

        return client;
    }

    #region User Management Methods

    /// <summary>
    /// Create and register a new test user with OTP verification
    /// </summary>
    protected async Task<TestUser> CreateAndRegisterUserAsync(TestUser? user = null)
    {
        user ??= TestUtilities.CreateTestUser();

        // Step 1: Register user
        await RegisterUserAsync(user);

        // Step 2: Verify OTP and complete registration
        await VerifyUserOtpAsync(user);

        _currentUser = user;
        return user;
    }

    /// <summary>
    /// Register user (first step of registration)
    /// </summary>
    protected async Task RegisterUserAsync(TestUser user)
    {
        var registerRequest = new
        {
            mobile = user.Mobile,
            password = user.Password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Register, registerRequest);
        response.ShouldBeNoContent();
    }

    /// <summary>
    /// Verify OTP and complete user registration
    /// </summary>
    protected async Task VerifyUserOtpAsync(TestUser user)
    {
        var verifyRequest = new
        {
            mobile = user.Mobile,
            otp = TestConfiguration.TestData.DefaultOtp
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.VerifyOtp, verifyRequest);
        var loginResult = await response.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        loginResult.ShouldBeSuccessfulLogin();
        user.Token = loginResult.Token!;

        // Update user ID from token or profile
        await UpdateUserProfileFromApiAsync(user);
    }

    /// <summary>
    /// Login user and set authentication token
    /// </summary>
    protected async Task<TestUser> LoginUserAsync(TestUser user)
    {
        var loginRequest = new
        {
            mobile = user.Mobile,
            password = user.Password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest);
        var loginResult = await response.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        loginResult.ShouldBeSuccessfulLogin();
        user.Token = loginResult.Token!;

        await UpdateUserProfileFromApiAsync(user);
        _currentUser = user;
        return user;
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    protected async Task UpdateUserProfileAsync(TestUser user)
    {
        SetAuthenticationToken(user.Token);

        var updateRequest = new
        {
            name = user.Name,
            family = user.Family,
            nationalCode = user.NationalCode,
            gender = 1, // Male
            birthDate = DateTime.Now.AddYears(-30)
        };

        var response = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);
        response.ShouldBeNoContent();
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    protected async Task<UserProfileResponse> GetCurrentUserProfileAsync()
    {
        EnsureAuthenticated();

        var response = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);
        return await response.EnsureSuccessAndReadAsJsonAsync<UserProfileResponse>();
    }

    private async Task UpdateUserProfileFromApiAsync(TestUser user)
    {
        SetAuthenticationToken(user.Token);
        var profile = await GetCurrentUserProfileAsync();
        user.Id = profile.Id;
    }

    #endregion

    #region Wallet Management Methods

    /// <summary>
    /// Create wallet for user (usually happens automatically during registration)
    /// </summary>
    protected async Task<WalletResponse> CreateWalletAsync()
    {
        EnsureAuthenticated();

        var response = await _httpClient.PostAsync(EndpointUrls.Wallet.CreateWallet, null);
        return await response.EnsureSuccessAndReadAsJsonAsync<WalletResponse>();
    }

    /// <summary>
    /// Get wallet balance
    /// </summary>
    protected async Task<WalletResponse> GetWalletBalanceAsync()
    {
        EnsureAuthenticated();

        var response = await _httpClient.GetAsync(EndpointUrls.Wallet.GetWalletBalance);
        return await response.EnsureSuccessAndReadAsJsonAsync<WalletResponse>();
    }

    /// <summary>
    /// Perform direct deposit to wallet
    /// </summary>
    protected async Task<DirectDepositResponse> PerformDirectDepositAsync(decimal amount, string description = "Test deposit")
    {
        EnsureAuthenticated();

        var depositRequest = new
        {
            amount = amount,
            description = description,
            callbackUrl = "https://test.callback.url"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.DirectDeposit, depositRequest);
        return await response.EnsureSuccessAndReadAsJsonAsync<DirectDepositResponse>();
    }

    /// <summary>
    /// Simulate payment gateway callback to complete deposit
    /// </summary>
    protected async Task CompleteDepositViaCallbackAsync(string authority, decimal amount)
    {
        var callbackRequest = new
        {
            authority = authority,
            status = "OK",
            amount = amount
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.PaymentCallback, callbackRequest);
        response.ShouldBeSuccessfulHttpResponse();
    }

    /// <summary>
    /// Add bank account to wallet
    /// </summary>
    protected async Task<BankAccountResponse> AddBankAccountAsync(string? bankName = null, string? accountNumber = null)
    {
        EnsureAuthenticated();

        var bankAccountRequest = new
        {
            bankName = bankName ?? TestConfiguration.TestData.TestBankName,
            accountNumber = accountNumber ?? TestUtilities.GenerateTestCardNumber()[..16],
            cardNumber = TestUtilities.GenerateTestCardNumber(),
            shabaNumber = TestUtilities.GenerateTestShabaNumber(),
            accountHolderName = $"{_currentUser?.Name} {_currentUser?.Family}"
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.AddBankAccount, bankAccountRequest);
        return await response.EnsureSuccessAndReadAsJsonAsync<BankAccountResponse>();
    }

    #endregion

    #region Order Management Methods

    /// <summary>
    /// Create a new order
    /// </summary>
    protected async Task<OrderResponse> CreateOrderAsync(List<TestPassenger>? passengers = null, string serviceType = "Train")
    {
        EnsureAuthenticated();
        passengers ??= TestUtilities.CreateTestPassengers(1);

        var orderRequest = new
        {
            serviceType = serviceType,
            sourceCode = 1, // Tehran
            destinationCode = 2, // Isfahan
            sourceName = "تهران",
            destinationName = "اصفهان",
            departureDate = DateTime.Now.AddDays(7),
            returnDate = (DateTime?)null,
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
        return await response.EnsureSuccessAndReadAsJsonAsync<OrderResponse>();
    }

    /// <summary>
    /// Save passenger information
    /// </summary>
    protected async Task<SavedPassengerResponse> SavePassengerAsync(TestPassenger passenger)
    {
        EnsureAuthenticated();

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
        return await response.EnsureSuccessAndReadAsJsonAsync<SavedPassengerResponse>();
    }

    #endregion

    #region Authentication Helpers

    protected void SetAuthenticationToken(string token)
    {
        _httpClient.AddAuthorizationHeader(token);
    }

    protected void ClearAuthentication()
    {
        _httpClient.RemoveAuthorizationHeader();
        _currentUser = null;
    }

    protected void EnsureAuthenticated()
    {
        if (_currentUser == null || string.IsNullOrEmpty(_currentUser.Token))
        {
            throw new InvalidOperationException("User must be authenticated before performing this operation");
        }

        SetAuthenticationToken(_currentUser.Token);
    }

    #endregion

    #region Utility Methods

    protected async Task WaitForTransactionCompletionAsync(Guid transactionId, TimeSpan? timeout = null)
    {
        timeout ??= TestConfiguration.Timeouts.DefaultTimeout;

        await TestUtilities.WaitForConditionAsync(async () =>
        {
            // Check transaction status via API
            // Implementation depends on available endpoints
            return true; // Placeholder
        }, timeout.Value);
    }

    protected async Task AssertWalletBalanceAsync(decimal expectedBalance, string operation = "")
    {
        var wallet = await GetWalletBalanceAsync();
        wallet.TotalBalanceInIrr.Should().Be(expectedBalance,
            $"Wallet balance should be {expectedBalance} after {operation}");
    }

    protected async Task AssertWalletBalanceChangedAsync(decimal previousBalance, decimal expectedChange, string operation)
    {
        var wallet = await GetWalletBalanceAsync();
        AssertionHelpers.ShouldHaveBalanceChanged(previousBalance, wallet.TotalBalanceInIrr, expectedChange, operation);
    }

    #endregion

    public virtual void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}