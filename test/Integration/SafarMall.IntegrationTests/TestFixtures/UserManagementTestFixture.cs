using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using System.Collections.Concurrent;

namespace SafarMall.IntegrationTests.TestFixtures;

/// <summary>
/// Test fixture for User Management service operations
/// Manages user lifecycle, authentication, and cleanup for tests
/// </summary>
public class UserManagementTestFixture : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentBag<TestUser> _createdUsers;
    private readonly object _lockObject = new();
    private bool _disposed = false;

    public UserManagementTestFixture()
    {
        _httpClient = CreateHttpClient();
        _createdUsers = new ConcurrentBag<TestUser>();
    }

    #region HTTP Client Setup

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "SafarMall-IntegrationTests/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TestConfiguration.Timeouts.DefaultTimeout;
        return client;
    }

    #endregion

    #region User Registration Methods

    /// <summary>
    /// Create a complete test user with registration and OTP verification
    /// </summary>
    public async Task<TestUser> CreateCompleteUserAsync(TestUser? userData = null)
    {
        userData ??= TestUtilities.CreateTestUser();

        try
        {
            // Step 1: Register user
            await RegisterUserAsync(userData);

            // Step 2: Verify OTP and get token
            await VerifyUserOtpAsync(userData);

            // Step 3: Update profile if needed
            if (!string.IsNullOrEmpty(userData.Name) || !string.IsNullOrEmpty(userData.Family))
            {
                await UpdateUserProfileAsync(userData);
            }

            // Track created user for cleanup
            _createdUsers.Add(userData);

            return userData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create complete user: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Register a user (first step of registration process)
    /// </summary>
    public async Task RegisterUserAsync(TestUser user)
    {
        var registerRequest = new
        {
            mobile = user.Mobile,
            password = user.Password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Register, registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.ReadAsStringAsync();
            throw new InvalidOperationException($"User registration failed: {response.StatusCode} - {errorContent}");
        }
    }

    /// <summary>
    /// Verify OTP and complete user registration
    /// </summary>
    public async Task VerifyUserOtpAsync(TestUser user)
    {
        var verifyRequest = new
        {
            mobile = user.Mobile,
            otp = TestConfiguration.TestData.DefaultOtp
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.VerifyOtp, verifyRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.ReadAsStringAsync();
            throw new InvalidOperationException($"OTP verification failed: {response.StatusCode} - {errorContent}");
        }

        var loginResult = await response.ReadAsJsonAsync<LoginResponse>();

        if (loginResult?.Success != true || string.IsNullOrEmpty(loginResult.Token))
        {
            throw new InvalidOperationException("OTP verification succeeded but no valid token received");
        }

        user.Token = loginResult.Token;

        // Get user ID from profile
        await UpdateUserIdFromProfileAsync(user);
    }

    /// <summary>
    /// Login existing user and get authentication token
    /// </summary>
    public async Task<TestUser> LoginUserAsync(string mobile, string password)
    {
        var loginRequest = new
        {
            mobile = mobile,
            password = password
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.Login, loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.ReadAsStringAsync();
            throw new InvalidOperationException($"User login failed: {response.StatusCode} - {errorContent}");
        }

        var loginResult = await response.ReadAsJsonAsync<LoginResponse>();

        if (loginResult?.Success != true || string.IsNullOrEmpty(loginResult.Token))
        {
            throw new InvalidOperationException("Login succeeded but no valid token received");
        }

        var user = new TestUser
        {
            Mobile = mobile,
            Password = password,
            Token = loginResult.Token
        };

        await UpdateUserIdFromProfileAsync(user);
        return user;
    }

    #endregion

    #region Profile Management

    /// <summary>
    /// Update user profile information
    /// </summary>
    public async Task UpdateUserProfileAsync(TestUser user)
    {
        if (string.IsNullOrEmpty(user.Token))
        {
            throw new InvalidOperationException("User must be authenticated to update profile");
        }

        SetAuthenticationHeader(user.Token);

        var updateRequest = new
        {
            name = user.Name,
            family = user.Family,
            nationalCode = user.NationalCode,
            gender = 1, // Default to male
            birthDate = DateTime.Now.AddYears(-30) // Default age
        };

        var response = await _httpClient.PutAsJsonAsync(EndpointUrls.UserManagement.UpdateProfile, updateRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.ReadAsStringAsync();
            throw new InvalidOperationException($"Profile update failed: {response.StatusCode} - {errorContent}");
        }
    }

    /// <summary>
    /// Get user profile information
    /// </summary>
    public async Task<UserProfileResponse> GetUserProfileAsync(string token)
    {
        SetAuthenticationHeader(token);

        var response = await _httpClient.GetAsync(EndpointUrls.UserManagement.CurrentUser);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.ReadAsStringAsync();
            throw new InvalidOperationException($"Get profile failed: {response.StatusCode} - {errorContent}");
        }

        var profile = await response.ReadAsJsonAsync<UserProfileResponse>();

        if (profile == null)
        {
            throw new InvalidOperationException("Profile response was null");
        }

        return profile;
    }

    /// <summary>
    /// Update user ID from their profile (after authentication)
    /// </summary>
    private async Task UpdateUserIdFromProfileAsync(TestUser user)
    {
        var profile = await GetUserProfileAsync(user.Token);
        user.Id = profile.Id;

        // Update other profile fields if they weren't set
        if (string.IsNullOrEmpty(user.Name)) user.Name = profile.Name ?? "";
        if (string.IsNullOrEmpty(user.Family)) user.Family = profile.Family ?? "";
    }

    #endregion

    #region Password Management

    /// <summary>
    /// Reset user password using mobile and OTP
    /// </summary>
    public async Task ResetUserPasswordAsync(string mobile, string newPassword)
    {
        // Step 1: Request password reset
        var resetRequest = new { mobile = mobile };
        var resetResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.ResetPassword, resetRequest);

        if (!resetResponse.IsSuccessStatusCode)
        {
            var errorContent = await resetResponse.ReadAsStringAsync();
            throw new InvalidOperationException($"Password reset request failed: {resetResponse.StatusCode} - {errorContent}");
        }

        // Step 2: Complete password reset with OTP
        var resetWithOtpRequest = new
        {
            mobile = mobile,
            otp = TestConfiguration.TestData.DefaultOtp,
            newPassword = newPassword
        };

        var resetOtpResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.UserManagement.ResetPassword, resetWithOtpRequest);

        if (!resetOtpResponse.IsSuccessStatusCode)
        {
            var errorContent = await resetOtpResponse.ReadAsStringAsync();
            throw new InvalidOperationException($"Password reset with OTP failed: {resetOtpResponse.StatusCode} - {errorContent}");
        }
    }

    #endregion

    #region Authentication Helpers

    /// <summary>
    /// Set authentication header for HTTP client
    /// </summary>
    public void SetAuthenticationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clear authentication header
    /// </summary>
    public void ClearAuthenticationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Validate that a user token is still valid
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            await GetUserProfileAsync(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Create multiple test users for bulk testing scenarios
    /// </summary>
    public async Task<List<TestUser>> CreateMultipleUsersAsync(int count)
    {
        var users = new List<TestUser>();
        var tasks = new List<Task<TestUser>>();

        for (int i = 0; i < count; i++)
        {
            // Create sequential users to avoid conflicts
            await Task.Delay(100); // Small delay to ensure unique mobile numbers
            tasks.Add(CreateCompleteUserAsync());
        }

        var results = await Task.WhenAll(tasks);
        users.AddRange(results);

        return users;
    }

    /// <summary>
    /// Get all created users for testing
    /// </summary>
    public IEnumerable<TestUser> GetAllCreatedUsers()
    {
        return _createdUsers.ToList();
    }

    #endregion

    #region Cleanup and Disposal

    /// <summary>
    /// Clean up all created test users
    /// Note: In a real scenario, you might want to soft-delete or mark test users
    /// </summary>
    public async Task CleanupAsync()
    {
        if (_disposed) return;

        try
        {
            // Note: Since there's no delete user endpoint in the provided code,
            // we'll just clear our tracking. In a real scenario, you'd implement cleanup.

            foreach (var user in _createdUsers)
            {
                // Could potentially deactivate users or mark them as test users
                // if such functionality exists in the API
            }

            _createdUsers.Clear();
        }
        catch (Exception ex)
        {
            // Log cleanup errors but don't throw
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Synchronous cleanup
                CleanupAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disposal cleanup: {ex.Message}");
            }

            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    ~UserManagementTestFixture()
    {
        Dispose(false);
    }

    #endregion
}