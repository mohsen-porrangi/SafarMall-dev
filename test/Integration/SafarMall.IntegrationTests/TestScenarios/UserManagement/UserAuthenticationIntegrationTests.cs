using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.UserManagement;

[Collection("Sequential")]
public class UserAuthenticationIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "1")]
    public async Task Should_Login_With_Mobile_And_Password_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication(); // Clear registration token

        // Act
        var loginRequest = new
        {
            mobile = testUser.Mobile,
            password = testUser.Password,
            otp = (string?)null
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest);
        var loginResult = await response.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert
        loginResult.ShouldBeSuccessfulLogin();
        loginResult.Token.Should().NotBeNullOrEmpty();

        // Verify token works for authenticated requests
        SetAuthenticationToken(loginResult.Token!);
        var profile = await GetCurrentUserProfileAsync();
        profile.Id.Should().Be(testUser.Id);
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "2")]
    public async Task Should_Login_With_Mobile_And_OTP_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();

        // Act - Step 1: Request OTP by sending mobile only
        var mobileOnlyRequest = new
        {
            mobile = testUser.Mobile,
            password = (string?)null,
            otp = (string?)null
        };

        var mobileResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, mobileOnlyRequest);
        var mobileResult = await mobileResponse.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert - Should indicate OTP sent
        mobileResult.Success.Should().BeFalse();
        mobileResult.NextStep.Should().Be("enter-otp");
        mobileResult.Message.Should().Contain("کد تأیید");

        // Act - Step 2: Login with OTP
        var otpLoginRequest = new
        {
            mobile = testUser.Mobile,
            password = (string?)null,
            otp = TestConfiguration.TestData.DefaultOtp
        };

        var otpResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, otpLoginRequest);
        var otpResult = await otpResponse.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert
        otpResult.ShouldBeSuccessfulLogin();
        otpResult.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "3")]
    public async Task Should_Reject_Login_With_Invalid_Password()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();

        // Act
        var invalidLoginRequest = new
        {
            mobile = testUser.Mobile,
            password = "WrongPassword123!",
            otp = (string?)null
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, invalidLoginRequest);

        // Assert
        response.ShouldBeUnauthorized();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().Contain("نامعتبر", "Error should indicate invalid credentials");
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "4")]
    public async Task Should_Reject_Login_With_Invalid_OTP()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();

        // Act - Request OTP first
        var mobileOnlyRequest = new
        {
            mobile = testUser.Mobile,
            password = (string?)null,
            otp = (string?)null
        };

        await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, mobileOnlyRequest);

        // Act - Try login with invalid OTP
        var invalidOtpRequest = new
        {
            mobile = testUser.Mobile,
            password = (string?)null,
            otp = "000000" // Invalid OTP
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, invalidOtpRequest);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "5")]
    public async Task Should_Handle_Non_Existent_User_Login_Attempt()
    {
        // Arrange
        var nonExistentMobile = TestUtilities.GenerateTestMobile();

        // Act
        var loginRequest = new
        {
            mobile = nonExistentMobile,
            password = (string?)null,
            otp = (string?)null
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest);
        var loginResult = await response.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert - Should indicate user needs to register
        loginResult.Success.Should().BeFalse();
        loginResult.NextStep.Should().Be("register");
        loginResult.Message.Should().Contain("ثبت‌ نام");
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "6")]
    public async Task Should_Maintain_Session_Across_Multiple_Requests()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();
        await LoginUserAsync(testUser);

        // Act - Make multiple authenticated requests
        var profile1 = await GetCurrentUserProfileAsync();
        var wallet1 = await GetWalletBalanceAsync();
        await Task.Delay(1000); // Wait a bit
        var profile2 = await GetCurrentUserProfileAsync();
        var wallet2 = await GetWalletBalanceAsync();

        // Assert - All requests should succeed with consistent data
        profile1.Id.Should().Be(profile2.Id);
        profile1.Mobile.Should().Be(testUser.Mobile);
        wallet1.UserId.Should().Be(wallet2.UserId);
        wallet1.UserId.Should().Be(testUser.Id);
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "7")]
    public async Task Should_Reject_Requests_Without_Authentication()
    {
        // Arrange
        ClearAuthentication();

        // Act - Try to access protected endpoints without authentication
        var profileResponse = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);
        var walletResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetWalletBalance);

        // Assert
        profileResponse.ShouldBeUnauthorized();
        walletResponse.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "8")]
    public async Task Should_Reject_Requests_With_Invalid_Token()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        SetAuthenticationToken(invalidToken);

        // Act
        var profileResponse = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);
        var walletResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetWalletBalance);

        // Assert
        profileResponse.ShouldBeUnauthorized();
        walletResponse.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "9")]
    public async Task Should_Handle_Concurrent_Login_Attempts()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();

        var loginRequest = new
        {
            mobile = testUser.Mobile,
            password = testUser.Password,
            otp = (string?)null
        };

        // Act - Perform multiple concurrent login attempts
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var httpClient = CreateHttpClient(); // Use separate clients for concurrency
            tasks.Add(httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed (assuming no rate limiting)
        foreach (var response in responses)
        {
            response.ShouldBeSuccessfulHttpResponse();
            var result = await response.ReadAsJsonAsync<LoginResponse>();
            result!.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
        }

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "10")]
    public async Task Should_Handle_Login_After_Password_Change()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var newPassword = "NewTest@123456";

        // Act - Update password
        var updateRequest = new
        {
            name = testUser.Name,
            family = testUser.Family,
            nationalCode = testUser.NationalCode,
            gender = 1,
            birthDate = DateTime.Now.AddYears(-30),
            currentPassword = testUser.Password,
            newPassword = newPassword
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);
        updateResponse.ShouldBeNoContent();

        // Clear authentication and try login with new password
        ClearAuthentication();
        testUser.Password = newPassword;

        // Act - Login with new password
        var loginRequest = new
        {
            mobile = testUser.Mobile,
            password = newPassword,
            otp = (string?)null
        };

        var loginResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest);
        var loginResult = await loginResponse.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert
        loginResult.ShouldBeSuccessfulLogin();

        // Verify old password doesn't work
        var oldPasswordRequest = new
        {
            mobile = testUser.Mobile,
            password = "Test@123456", // Original password
            otp = (string?)null
        };

        var oldPasswordResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, oldPasswordRequest);
        oldPasswordResponse.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserAuthentication")]
    [Trait("Priority", "11")]
    public async Task Should_Handle_Authentication_Cross_Service_Calls()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        ClearAuthentication();
        await LoginUserAsync(testUser);

        // Act - Test authentication across different services
        var userProfile = await GetCurrentUserProfileAsync(); // UserManagement service
        var walletBalance = await GetWalletBalanceAsync(); // Wallet service

        var passengers = TestUtilities.CreateTestPassengers(1);
        var order = await CreateOrderAsync(passengers); // Order service

        // Assert - All services should recognize the same authentication token
        userProfile.Id.Should().Be(testUser.Id);
        walletBalance.UserId.Should().Be(testUser.Id);
        order.Should().NotBeNull();

        // All should belong to the same user
        walletBalance.UserId.Should().Be(userProfile.Id);
    }
}