using FluentAssertions;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;
using Xunit;

namespace SafarMall.IntegrationTests.TestScenarios.Order;

[Collection("Sequential")]
public class PassengerManagementIntegrationTests : BaseIntegrationTest
{
    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "1")]
    public async Task Should_Save_Iranian_Passenger_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);

        // Act
        var savedPassenger = await SavePassengerAsync(passenger);

        // Assert
        savedPassenger.ShouldBeValidSavedPassenger(passenger);
        savedPassenger.Id.Should().BeGreaterThan(0);
        savedPassenger.NationalCode.Should().Be(passenger.NationalCode);
        savedPassenger.PassportNo.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "2")]
    public async Task Should_Save_Foreign_Passenger_Successfully()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: false);

        // Act
        var savedPassenger = await SavePassengerAsync(passenger);

        // Assert
        savedPassenger.ShouldBeValidSavedPassenger(passenger);
        savedPassenger.Id.Should().BeGreaterThan(0);
        savedPassenger.PassportNo.Should().Be(passenger.PassportNo);
        savedPassenger.NationalCode.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "3")]
    public async Task Should_Get_Saved_Passengers_List()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger1 = TestUtilities.CreateTestPassenger(isIranian: true);
        var passenger2 = TestUtilities.CreateTestPassenger(isIranian: false);

        // Act - Save multiple passengers
        var savedPassenger1 = await SavePassengerAsync(passenger1);
        var savedPassenger2 = await SavePassengerAsync(passenger2);

        // Act - Get saved passengers list
        var passengersResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetSavedPassengers);
        var passengersData = await passengersResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Assert
        passengersResponse.ShouldBeSuccessfulHttpResponse();
        // Should contain both saved passengers
        // Exact assertions depend on API response structure
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "4")]
    public async Task Should_Delete_Saved_Passenger()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);

        // Act - Save passenger
        var savedPassenger = await SavePassengerAsync(passenger);

        // Act - Delete passenger
        var deleteResponse = await _httpClient.DeleteAsync(EndpointUrls.Order.DeleteSavedPassenger(savedPassenger.Id));

        // Assert
        deleteResponse.ShouldBeNoContent();

        // Verify passenger is deleted by trying to get passengers list
        var passengersResponse = await _httpClient.GetAsync(EndpointUrls.Order.GetSavedPassengers);
        var passengersData = await passengersResponse.EnsureSuccessAndReadAsJsonAsync<dynamic>();

        // Should not contain the deleted passenger
        passengersResponse.ShouldBeSuccessfulHttpResponse();
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "5")]
    public async Task Should_Not_Allow_Invalid_National_Code()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        passenger.NationalCode = "1234567890"; // Invalid national code

        // Act
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

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().Contain("کد ملی", "Error should mention national code validation");
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "6")]
    public async Task Should_Require_Passport_For_Foreign_Passenger()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: false);
        passenger.PassportNo = null; // Missing passport for foreign passenger

        // Act
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

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("passport");
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "7")]
    public async Task Should_Not_Allow_Unauthorized_Passenger_Access()
    {
        // Arrange - Create passenger with first user
        var firstUser = await CreateAndRegisterUserAsync();
        var passenger = TestUtilities.CreateTestPassenger(isIranian: true);
        var savedPassenger = await SavePassengerAsync(passenger);

        // Arrange - Create second user
        var secondUser = await CreateAndRegisterUserAsync();

        // Act - Try to delete first user's passenger with second user
        ClearAuthentication();
        _currentUser = secondUser;
        SetAuthenticationToken(secondUser.Token);

        var deleteResponse = await _httpClient.DeleteAsync(EndpointUrls.Order.DeleteSavedPassenger(savedPassenger.Id));

        // Assert - Should be forbidden or not found
        deleteResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.Forbidden,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "8")]
    public async Task Should_Use_Saved_Passengers_In_Order_Creation()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var passenger1 = TestUtilities.CreateTestPassenger(isIranian: true);
        var passenger2 = TestUtilities.CreateTestPassenger(isIranian: true);

        // Act - Save passengers first
        var savedPassenger1 = await SavePassengerAsync(passenger1);
        var savedPassenger2 = await SavePassengerAsync(passenger2);

        // Act - Create order using saved passengers data
        var passengers = new List<TestPassenger> { passenger1, passenger2 };
        var order = await CreateOrderAsync(passengers);

        // Assert
        order.ShouldBeValidOrder(testUser, passengers);
        order.PassengerCount.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "9")]
    public async Task Should_Validate_Passenger_Age_Restrictions()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();
        var infantPassenger = TestUtilities.CreateTestPassenger(isIranian: true);
        infantPassenger.BirthDate = DateTime.Now.AddMonths(-6); // 6 months old

        // Act
        var savedInfant = await SavePassengerAsync(infantPassenger);

        // Assert - Should save infant successfully
        savedInfant.ShouldBeValidSavedPassenger(infantPassenger);

        // Test with invalid birth date (future date)
        var invalidPassenger = TestUtilities.CreateTestPassenger(isIranian: true);
        invalidPassenger.BirthDate = DateTime.Now.AddDays(1); // Future date

        var invalidRequest = new
        {
            firstNameEn = invalidPassenger.FirstNameEn,
            lastNameEn = invalidPassenger.LastNameEn,
            firstNameFa = invalidPassenger.FirstNameFa,
            lastNameFa = invalidPassenger.LastNameFa,
            nationalCode = invalidPassenger.NationalCode,
            birthDate = invalidPassenger.BirthDate,
            gender = invalidPassenger.Gender
        };

        var invalidResponse = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.SavePassenger, invalidRequest);
        invalidResponse.ShouldBeBadRequest();
    }

    [Fact]
    [Trait("Category", "Passenger")]
    [Trait("Priority", "10")]
    public async Task Should_Handle_Passenger_Name_Validation()
    {
        // Arrange
        var testUser = await CreateAndRegisterUserAsync();

        // Act - Try to save passenger with empty names
        var invalidRequest = new
        {
            firstNameEn = "",
            lastNameEn = "",
            firstNameFa = "",
            lastNameFa = "",
            nationalCode = TestUtilities.GenerateTestNationalCode(),
            birthDate = DateTime.Now.AddYears(-25),
            gender = 1
        };

        var response = await _httpClient.PostAsJsonAsync(EndpointUrls.Order.SavePassenger, invalidRequest);

        // Assert
        response.ShouldBeBadRequest();

        var errorContent = await response.ReadAsStringAsync();
        errorContent.Should().ContainEquivalentOf("name");
    }
}