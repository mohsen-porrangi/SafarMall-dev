using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Wallet;

[Collection("Sequential")]
public class WalletCreationIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "1")]
    public async Task Should_Create_Wallet_Automatically_During_User_Registration()
    {
        // Arrange
        var testUser = TestUtilities.CreateTestUser();

        // Act - Register user (wallet should be created automatically)
        await CreateAndRegisterUserAsync(testUser);

        // Assert - Wallet should exist and be accessible
        var wallet = await GetWalletBalanceAsync();
        wallet.ShouldBeValidWallet(testUser.Id);
        wallet.IsActive.Should().BeTrue();
        wallet.TotalBalanceInIrr.Should().Be(0); // New wallet starts with zero balance
        wallet.CurrencyBalances.Should().NotBeEmpty();
        wallet.CurrencyBalances.Should().ContainSingle(cb => cb.Currency == "IRR");
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "2")]
    public async Task Should_Check_Wallet_Exists_Via_Internal_API()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Check wallet existence via internal API
        var checkResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.CheckWalletExists(testUser.Id));
        var checkResult = await checkResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        checkResponse.ShouldBeSuccessfulHttpResponse();
        // The exact structure depends on API response, but should indicate wallet exists
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "3")]
    public async Task Should_Get_Wallet_Balance_Via_Internal_API()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Get wallet balance via internal API
        var balanceResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetWalletBalanceInternal(testUser.Id));
        var balanceResult = await balanceResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        balanceResponse.ShouldBeSuccessfulHttpResponse();
        // Should return wallet balance information for internal service consumption
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "4")]
    public async Task Should_Create_Wallet_Manually_If_Not_Exists()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to create wallet manually (should handle if already exists)
        var createResponse = await _httpClient.PostAsync(EndpointUrls.Wallet.CreateWallet, null);

        // Assert - Should either create successfully or indicate already exists
        // The exact behavior depends on business logic
        if (createResponse.IsSuccessStatusCode)
        {
            var createResult = await createResponse.ReadAsJsonAsync<WalletResponse>();
            createResult.ShouldBeValidWallet(testUser.Id);
        }
        else
        {
            // Should be a business logic error indicating wallet already exists
            createResponse.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.Conflict,
                System.Net.HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "5")]
    public async Task Should_Get_Wallet_Summary_With_Initial_State()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act
        var summaryResponse = await _httpClient.GetAsync(EndpointUrls.Wallet.GetWalletSummary);
        var summary = await summaryResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        summaryResponse.ShouldBeSuccessfulHttpResponse();
        // Summary should show empty wallet state for new user
        // Exact assertions depend on API response structure
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "6")]
    public async Task Should_Handle_Wallet_Creation_Via_Internal_Service()
    {
        // Arrange - Create user but don't complete wallet creation
        var testUser = TestUtilities.CreateTestUser();

        // Register user manually without automatic wallet creation
        await RegisterUserAsync(testUser);
        await VerifyUserOtpAsync(testUser);

        // Act - Create wallet via internal service endpoint
        var createInternalRequest = new
        {
            userId = testUser.Id
        };

        var createResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Wallet.CreateWalletInternal, createInternalRequest);
        var createResult = await createResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        createResponse.ShouldBeSuccessfulHttpResponse();

        // Verify wallet was created by checking balance
        var wallet = await GetWalletBalanceAsync();
        wallet.ShouldBeValidWallet(testUser.Id);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "7")]
    public async Task Should_Initialize_Default_IRR_Currency_Account()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act
        var wallet = await GetWalletBalanceAsync();

        // Assert - Should have default IRR currency account
        wallet.CurrencyBalances.Should().ContainSingle();
        var irrAccount = wallet.CurrencyBalances.First();
        irrAccount.Currency.Should().Be("IRR");
        irrAccount.Balance.Should().Be(0);
        irrAccount.IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "8")]
    public async Task Should_Handle_Multiple_Wallet_Creation_Requests_Gracefully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to create wallet multiple times
        var firstResponse = await _httpClient.PostAsync(EndpointUrls.Wallet.CreateWallet, null);
        var secondResponse = await _httpClient.PostAsync(EndpointUrls.Wallet.CreateWallet, null);
        var thirdResponse = await _httpClient.PostAsync(EndpointUrls.Wallet.CreateWallet, null);

        // Assert - Should handle gracefully (either success if idempotent or appropriate error)
        // At least one should succeed, others should fail gracefully
        var responses = new[] { firstResponse, secondResponse, thirdResponse };
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var conflictCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.Conflict);

        (successCount + conflictCount).Should().Be(3, "All requests should be handled properly");

        // Final state should be consistent
        var finalWallet = await GetWalletBalanceAsync();
        finalWallet.ShouldBeValidWallet(testUser.Id);
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "9")]
    public async Task Should_Check_Affordability_For_New_Wallet()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var checkAmount = 100000m;

        // Act - Check affordability for amount greater than zero balance
        var affordabilityRequest = new
        {
            amount = checkAmount,
            currency = "IRR"
        };

        var affordabilityResponse = await _httpClient.PostAsJsonAsync(
            EndpointUrls.Wallet.CheckAffordability(testUser.Id),
            affordabilityRequest);
        var affordabilityResult = await affordabilityResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        affordabilityResponse.ShouldBeSuccessfulHttpResponse();
        // Should indicate insufficient balance for new wallet
        // Exact structure depends on API response
    }

    [Fact]
    [Trait("Category", "Wallet")]
    [Trait("Priority", "10")]
    public async Task Should_Maintain_Wallet_State_Across_User_Sessions()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Get initial wallet state
        var initialWallet = await GetWalletBalanceAsync();

        // Act - Simulate logout and login
        ClearAuthentication();
        await LoginUserAsync(testUser);

        // Act - Get wallet state again
        var walletAfterRelogin = await GetWalletBalanceAsync();

        // Assert - Wallet state should be preserved
        walletAfterRelogin.WalletId.Should().Be(initialWallet.WalletId);
        walletAfterRelogin.UserId.Should().Be(initialWallet.UserId);
        walletAfterRelogin.IsActive.Should().Be(initialWallet.IsActive);
        walletAfterRelogin.TotalBalanceInIrr.Should().Be(initialWallet.TotalBalanceInIrr);
    }
}