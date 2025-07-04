using FluentAssertions;
using SafarMall.IntegrationTests.Models;
using System.Net;

namespace SafarMall.IntegrationTests.Helpers;

public static class AssertionHelpers
{
    public static void ShouldBeSuccessfulLogin(this LoginResponse response, string? expectedNextStep = null)
    {
        response.Should().NotBeNull();

        if (expectedNextStep == null)
        {
            response.Success.Should().BeTrue();
            response.Token.Should().NotBeNullOrEmpty();
        }
        else
        {
            response.Success.Should().BeFalse();
            response.NextStep.Should().Be(expectedNextStep);
            response.Message.Should().NotBeNullOrEmpty();
        }
    }

    public static void ShouldBeValidUserProfile(this UserProfileResponse response, TestUser expectedUser)
    {
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.Mobile.Should().Be(expectedUser.Mobile);
        response.IsActive.Should().BeTrue();

        if (!string.IsNullOrEmpty(expectedUser.Name))
        {
            response.Name.Should().Be(expectedUser.Name);
        }

        if (!string.IsNullOrEmpty(expectedUser.Family))
        {
            response.Family.Should().Be(expectedUser.Family);
        }
    }

    public static void ShouldBeValidWallet(this WalletResponse response, Guid expectedUserId)
    {
        response.Should().NotBeNull();
        response.WalletId.Should().NotBeEmpty();
        response.UserId.Should().Be(expectedUserId);
        response.IsActive.Should().BeTrue();
        response.TotalBalanceInIrr.Should().BeGreaterThanOrEqualTo(0);
        response.CurrencyBalances.Should().NotBeNull();
    }

    public static void ShouldBeValidTransaction(this TransactionResponse response, decimal expectedAmount, string expectedType, string expectedDirection)
    {
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.TransactionNumber.Should().NotBeNullOrEmpty();
        response.Amount.Should().Be(expectedAmount);
        response.Type.Should().Be(expectedType);
        response.Direction.Should().Be(expectedDirection);
        response.Currency.Should().Be("IRR");
        response.Status.Should().BeOneOf("Pending", "Completed", "Processing");
        response.Description.Should().NotBeNullOrEmpty();
    }

    public static void ShouldBeValidBankAccount(this BankAccountResponse response, string expectedBankName)
    {
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.BankName.Should().Be(expectedBankName);
        response.MaskedAccountNumber.Should().NotBeNullOrEmpty();
        response.MaskedAccountNumber.Should().Contain("****");
    }

    public static void ShouldBeValidOrder(this OrderResponse response, TestUser user, List<TestPassenger> passengers)
    {
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.OrderNumber.Should().NotBeNullOrEmpty();
        response.ServiceType.Should().NotBeNullOrEmpty();
        response.TotalAmount.Should().BeGreaterThan(0);
        response.Status.Should().BeOneOf("Pending", "Processing", "Completed", "Cancelled");
        response.PassengerCount.Should().Be(passengers.Count);
    }

    public static void ShouldBeValidSavedPassenger(this SavedPassengerResponse response, TestPassenger expectedPassenger)
    {
        response.Should().NotBeNull();
        response.Id.Should().BeGreaterThan(0);
        response.FirstNameEn.Should().Be(expectedPassenger.FirstNameEn);
        response.LastNameEn.Should().Be(expectedPassenger.LastNameEn);
        response.FirstNameFa.Should().Be(expectedPassenger.FirstNameFa);
        response.LastNameFa.Should().Be(expectedPassenger.LastNameFa);

        if (expectedPassenger.IsIranian)
        {
            response.NationalCode.Should().Be(expectedPassenger.NationalCode);
        }
        else
        {
            response.PassportNo.Should().Be(expectedPassenger.PassportNo);
        }
    }

    public static void ShouldBeValidDepositResponse(this DirectDepositResponse response)
    {
        response.Should().NotBeNull();
        response.IsSuccessful.Should().BeTrue();
        response.PaymentUrl.Should().NotBeNullOrEmpty();
        response.Authority.Should().NotBeNullOrEmpty();
        response.PendingTransactionId.Should().NotBeNull();
        response.PendingTransactionId.Should().NotBeEmpty();
    }

    public static void ShouldBeSuccessfulHttpResponse(this HttpResponseMessage response)
    {
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue($"Expected successful status code but got {response.StatusCode}");
    }

    public static void ShouldBeHttpStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(expectedStatusCode);
    }

    public static void ShouldBeUnauthorized(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.Unauthorized);
    }

    public static void ShouldBeBadRequest(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.BadRequest);
    }

    public static void ShouldBeNotFound(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.NotFound);
    }

    public static void ShouldBeForbidden(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.Forbidden);
    }

    public static void ShouldBeCreated(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.Created);
    }

    public static void ShouldBeNoContent(this HttpResponseMessage response)
    {
        response.ShouldBeHttpStatusCode(HttpStatusCode.NoContent);
    }

    public static void ShouldHaveBalanceChanged(decimal beforeBalance, decimal afterBalance, decimal expectedChange, string operation)
    {
        var actualChange = afterBalance - beforeBalance;

        if (operation.ToLower().Contains("deposit") || operation.ToLower().Contains("refund"))
        {
            actualChange.Should().Be(Math.Abs(expectedChange), $"Balance should increase by {expectedChange} after {operation}");
        }
        else if (operation.ToLower().Contains("purchase") || operation.ToLower().Contains("withdraw"))
        {
            actualChange.Should().Be(-Math.Abs(expectedChange), $"Balance should decrease by {expectedChange} after {operation}");
        }
        else
        {
            actualChange.Should().Be(expectedChange, $"Balance change should be {expectedChange} after {operation}");
        }
    }

    public static void ShouldBeValidGuid(string guidString, string fieldName = "GUID")
    {
        Guid.TryParse(guidString, out _).Should().BeTrue($"{fieldName} should be a valid GUID format");
    }

    public static void ShouldBeValidTransactionNumber(string transactionNumber)
    {
        transactionNumber.Should().NotBeNullOrEmpty();
        transactionNumber.Should().StartWith("TXN-");
        transactionNumber.Length.Should().BeGreaterThan(10);
    }

    public static void ShouldBeValidOrderNumber(string orderNumber)
    {
        orderNumber.Should().NotBeNullOrEmpty();
        orderNumber.Should().Contain("-");
        orderNumber.Length.Should().BeGreaterThan(5);
    }

    public static void ShouldBeValidIranianMobile(string mobile)
    {
        mobile.Should().NotBeNullOrEmpty();
        mobile.Should().MatchRegex(@"^09\d{9}$", "Mobile should be in format 09xxxxxxxxx");
    }

    public static void ShouldBeValidAmount(decimal amount, decimal? minAmount = null, decimal? maxAmount = null)
    {
        amount.Should().BeGreaterThan(0, "Amount should be positive");

        if (minAmount.HasValue)
        {
            amount.Should().BeGreaterThanOrEqualTo(minAmount.Value, $"Amount should be at least {minAmount}");
        }

        if (maxAmount.HasValue)
        {
            amount.Should().BeLessThanOrEqualTo(maxAmount.Value, $"Amount should not exceed {maxAmount}");
        }
    }
}