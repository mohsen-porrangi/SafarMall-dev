using FluentAssertions;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.UserManagement;

[Collection("Sequential")] // Ensure tests run sequentially to avoid conflicts
public class UserRegistrationIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "1")]
    public async Task Should_Register_User_Successfully_With_OTP_Verification()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act & Assert - Step 1: Initial Registration
        await RegisterUserAsync(testUser);

        // Act & Assert - Step 2: OTP Verification
        await VerifyUserOtpAsync(testUser);

        // Assert - User should be properly registered with valid token
        testUser.Token.Should().NotBeNullOrEmpty();
        testUser.Id.Should().NotBeEmpty();

        // Assert - User profile should be accessible
        var profile = await GetCurrentUserProfileAsync();
        profile.ShouldBeValidUserProfile(testUser);
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "2")]
    public async Task Should_Create_Wallet_Automatically_During_Registration()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Complete user registration
        await CreateAndRegisterUserAsync(testUser);

        // Assert - Wallet should be created automatically
        var wallet = await GetWalletBalanceAsync();
        wallet.ShouldBeValidWallet(testUser.Id);
        wallet.TotalBalanceInIrr.Should().Be(0); // New wallet should have zero balance
        wallet.CurrencyBalances.Should().ContainSingle(cb => cb.Currency == "IRR");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "3")]
    public async Task Should_Not_Allow_Duplicate_Mobile_Registration()
    {
        // Arrange
        var firstUser = TestUtilities.CreateTestUser();
        var duplicateUser = TestUtilities.CreateTestUser();
        duplicateUser.Mobile = firstUser.Mobile; // Same mobile number

        // Act - Register first user successfully
        await CreateAndRegisterUserAsync(firstUser);

        // Act & Assert - Try to register second user with same mobile
        var registerRequest = new
        {
            mobile = duplicateUser.Mobile,
            password = duplicateUser.Password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Register, registerRequest);
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().Contain("تکراری", "Error message should indicate duplicate mobile");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "4")]
    public async Task Should_Reject_Invalid_OTP()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Register user
        await RegisterUserAsync(testUser);

        // Act & Assert - Try to verify with invalid OTP
        var invalidOtpRequest = new
        {
            mobile = testUser.Mobile,
            otp = "000000" // Invalid OTP
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.VerifyOtp, invalidOtpRequest);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "5")]
    public async Task Should_Handle_Login_Flow_For_New_User()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Step 1: Try login with mobile only (should trigger OTP)
        var mobileOnlyRequest = new
        {
            mobile = testUser.Mobile,
            password = (string?)null,
            otp = (string?)null
        };

        var mobileResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, mobileOnlyRequest);
        var mobileResult = await mobileResponse.EnsureSuccessAndReadAsJsonAsync<LoginResponse>();

        // Assert - Should indicate user needs to register
        mobileResult.Success.Should().BeFalse();
        mobileResult.NextStep.Should().Be("register");
        mobileResult.Message.Should().Contain("ثبت‌ نام");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "6")]
    public async Task Should_Update_User_Profile_After_Registration()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Register user
        await CreateAndRegisterUserAsync(testUser);

        // Act - Update profile
        await UpdateUserProfileAsync(testUser);

        // Assert - Profile should be updated
        var updatedProfile = await GetCurrentUserProfileAsync();
        updatedProfile.Name.Should().Be(testUser.Name);
        updatedProfile.Family.Should().Be(testUser.Family);
        updatedProfile.NationalId.Should().Be(testUser.NationalCode);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "7")]
    public async Task Should_Maintain_Session_After_Registration()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Register user
        await CreateAndRegisterUserAsync(testUser);

        // Act - Make multiple authenticated requests
        var profile1 = await GetCurrentUserProfileAsync();
        var wallet1 = await GetWalletBalanceAsync();
        var profile2 = await GetCurrentUserProfileAsync();

        // Assert - All requests should succeed with same user data
        profile1.Id.Should().Be(profile2.Id);
        profile1.Mobile.Should().Be(testUser.Mobile);
        wallet1.UserId.Should().Be(testUser.Id);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "8")]
    public async Task Should_Handle_Password_Reset_Flow()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();
        await CreateAndRegisterUserAsync(testUser);
        ClearAuthentication(); // Simulate forgot password scenario

        // Act - Step 1: Request password reset
        var resetRequest = new
        {
            mobile = testUser.Mobile
        };

        var resetResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.ResetPassword, resetRequest);
        resetResponse.ShouldBeNoContent();

        // Act - Step 2: Reset password with OTP
        var newPassword = "NewTest@123456";
        var resetWithOtpRequest = new
        {
            mobile = testUser.Mobile,
            otp = TestConfiguration.TestData.DefaultOtp,
            newPassword = newPassword
        };

        var resetOtpResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.ResetPassword, resetWithOtpRequest);
        resetOtpResponse.ShouldBeNoContent();

        // Assert - Should be able to login with new password
        testUser.Password = newPassword;
        await LoginUserAsync(testUser);

        var profile = await GetCurrentUserProfileAsync();
        profile.Id.Should().Be(testUser.Id);
    }
}