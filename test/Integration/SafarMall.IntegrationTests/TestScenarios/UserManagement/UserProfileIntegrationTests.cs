using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.UserManagement;

[Collection("Sequential")]
public class UserProfileIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "1")]
    public async Task Should_Get_Current_User_Profile_After_Registration()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act
        var profile = await GetCurrentUserProfileAsync();

        // Assert
        profile.ShouldBeValidUserProfile(testUser);
        profile.Id.Should().Be(testUser.Id);
        profile.Mobile.Should().Be(testUser.Mobile);
        profile.IsActive.Should().BeTrue();
        profile.Name.Should().BeNullOrEmpty(); // Initially empty after registration
        profile.Family.Should().BeNullOrEmpty(); // Initially empty after registration
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "2")]
    public async Task Should_Update_User_Profile_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var updatedName = "احمد";
        var updatedFamily = "محمدی";
        var updatedNationalCode = TestUtilities.GenerateTestNationalCode();

        // Act
        var updateRequest = new
        {
            name = updatedName,
            family = updatedFamily,
            nationalCode = updatedNationalCode,
            gender = 1, // Male
            birthDate = DateTime.Now.AddYears(-30)
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);
        updateResponse.ShouldBeNoContent();

        // Assert - Get updated profile
        var updatedProfile = await GetCurrentUserProfileAsync();
        updatedProfile.Name.Should().Be(updatedName);
        updatedProfile.Family.Should().Be(updatedFamily);
        updatedProfile.NationalId.Should().Be(updatedNationalCode);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "3")]
    public async Task Should_Update_Password_Through_Profile()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var currentPassword = testUser.Password;
        var newPassword = "NewTest@789456";

        // Act - Update profile with password change
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            gender = 1,
            birthDate = DateTime.Now.AddYears(-25),
            currentPassword = currentPassword,
            newPassword = newPassword
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);
        updateResponse.ShouldBeNoContent();

        // Assert - Should be able to login with new password
        ClearAuthentication();
        testUser.Password = newPassword;
        await LoginUserAsync(testUser);

        var profile = await GetCurrentUserProfileAsync();
        profile.Id.Should().Be(testUser.Id);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "4")]
    public async Task Should_Reject_Invalid_National_Code()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to update with invalid national code
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر",
            nationalCode = "1234567890", // Invalid national code
            gender = 1,
            birthDate = DateTime.Now.AddYears(-25)
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);

        // Assert
        updateResponse.ShouldBeBadRequest();

        var errorContent = await updateResponse.ReadAsStringAsync();
        errorContent.Should().Contain("کد ملی", "Error should mention national code validation");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "5")]
    public async Task Should_Reject_Duplicate_National_Code()
    {
        // Arrange - Create two users
        var firstUser = await CreateAndRegisterUserAsync();
        var nationalCodeToUse = TestUtilities.GenerateTestNationalCode();

        // Update first user with national code
        var firstUpdateRequest = new
        {
            name = "کاربر اول",
            family = "تست",
            nationalCode = nationalCodeToUse,
            gender = 1,
            birthDate = DateTime.Now.AddYears(-30)
        };

        var firstUpdateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, firstUpdateRequest);
        firstUpdateResponse.ShouldBeNoContent();

        // Create second user
        var secondUser = await CreateAndRegisterUserAsync();

        // Act - Try to update second user with same national code
        var secondUpdateRequest = new
        {
            name = "کاربر دوم",
            family = "تست",
            nationalCode = nationalCodeToUse, // Same national code
            gender = 2,
            birthDate = DateTime.Now.AddYears(-25)
        };

        var secondUpdateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, secondUpdateRequest);

        // Assert
        secondUpdateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

        var errorContent = await secondUpdateResponse.ReadAsStringAsync();
        errorContent.Should().Contain("تکراری", "Error should mention duplicate national code");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "6")]
    public async Task Should_Reject_Wrong_Current_Password()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to change password with wrong current password
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            gender = 1,
            birthDate = DateTime.Now.AddYears(-25),
            currentPassword = "WrongPassword123",
            newPassword = "NewPassword456"
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);

        // Assert
        updateResponse.ShouldBeUnauthorized();

        var errorContent = await updateResponse.ReadAsStringAsync();
        errorContent.Should().Contain("رمز", "Error should mention password validation");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "7")]
    public async Task Should_Validate_Birth_Date()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to update with future birth date
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            gender = 1,
            birthDate = DateTime.Now.AddYears(1) // Future date
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);

        // Assert
        updateResponse.ShouldBeBadRequest();

        var errorContent = await updateResponse.ReadAsStringAsync();
        errorContent.Should().Contain("تاریخ تولد", "Error should mention birth date validation");
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "8")]
    public async Task Should_Require_Authentication_For_Profile_Access()
    {
        // Arrange - No authentication

        // Act - Try to get profile without authentication
        var profileResponse = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);

        // Assert
        profileResponse.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "9")]
    public async Task Should_Require_Authentication_For_Profile_Update()
    {
        // Arrange - No authentication
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر"
        };

        // Act - Try to update profile without authentication
        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);

        // Assert
        updateResponse.ShouldBeUnauthorized();
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "10")]
    public async Task Should_Handle_Partial_Profile_Updates()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // First, set initial profile data
        var initialRequest = new
        {
            name = "نام اولیه",
            family = "خانوادگی اولیه",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            gender = 1,
            birthDate = DateTime.Now.AddYears(-30)
        };

        var initialResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, initialRequest);
        initialResponse.ShouldBeNoContent();

        // Act - Update only name and family
        var partialUpdateRequest = new
        {
            name = "نام جدید",
            family = "خانوادگی جدید",
            nationalCode = initialRequest.nationalCode, // Keep same
            gender = 1,
            birthDate = initialRequest.birthDate // Keep same
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, partialUpdateRequest);
        updateResponse.ShouldBeNoContent();

        // Assert - Only name and family should be updated
        var updatedProfile = await GetCurrentUserProfileAsync();
        updatedProfile.Name.Should().Be("نام جدید");
        updatedProfile.Family.Should().Be("خانوادگی جدید");
        updatedProfile.NationalId.Should().Be(initialRequest.nationalCode);
    }

    [Fact]
    [Trait("Category", "UserManagement")]
    [Trait("Priority", "11")]
    public async Task Should_Maintain_Session_After_Profile_Update()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Update profile
        var updateRequest = new
        {
            name = "تست",
            family = "کاربر",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            gender = 1,
            birthDate = DateTime.Now.AddYears(-25)
        };

        var updateResponse = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);
        updateResponse.ShouldBeNoContent();

        // Assert - Should still be able to access profile with same token
        var profile1 = await GetCurrentUserProfileAsync();
        var wallet = await GetWalletBalanceAsync();
        var profile2 = await GetCurrentUserProfileAsync();

        // Both profile calls should succeed and return same data
        profile1.Id.Should().Be(profile2.Id);
        profile1.Name.Should().Be(updateRequest.name);
        wallet.UserId.Should().Be(testUser.Id);
    }
}