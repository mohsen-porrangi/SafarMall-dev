using Bogus;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Models;

namespace SafarMall.IntegrationTests.Helpers;

public static class TestUtilities
{
    private static readonly Faker _faker = new("fa");

    public static string GenerateTestMobile()
    {
        // Generate Iranian mobile number format: 09xxxxxxxxx
        return $"09{_faker.Random.Number(100000000, 999999999)}";
    }

    public static string GenerateTestNationalCode()
    {
        // Generate valid Iranian national code
        var digits = new int[10];

        // Generate first 9 digits
        for (int i = 0; i < 9; i++)
        {
            digits[i] = _faker.Random.Number(0, 9);
        }

        // Calculate check digit
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += digits[i] * (10 - i);
        }

        var remainder = sum % 11;
        digits[9] = remainder < 2 ? remainder : 11 - remainder;

        return string.Join("", digits);
    }

    public static string GenerateTestCardNumber()
    {
        // Generate valid Iranian bank card number (Luhn algorithm)
        var cardNumber = "6104337650";
        var remaining = _faker.Random.Number(100000, 999999).ToString();

        var fullNumber = cardNumber + remaining;
        var checkDigit = CalculateLuhnCheckDigit(fullNumber);

        return fullNumber + checkDigit;
    }

    public static string GenerateTestShabaNumber()
    {
        // Generate valid Iranian SHABA number
        var bankCode = _faker.Random.Number(100, 999).ToString();
        var accountNumber = _faker.Random.Long(1000000000L, 9999999999L).ToString();
        var baseShaba = $"IR00{bankCode}{accountNumber}";

        // Calculate check digits (simplified)
        var checkDigits = CalculateShabaCheckDigits(baseShaba);
        return $"IR{checkDigits:D2}{bankCode}{accountNumber}";
    }

    public static TestUser CreateTestUser()
    {
        return new TestUser
        {
            Id = Guid.NewGuid(),
            Mobile = GenerateTestMobile(),
            Password = TestConfiguration.TestData.TestPassword,
            Name = _faker.Name.FirstName(),
            Family = _faker.Name.LastName(),
            NationalCode = GenerateTestNationalCode()
        };
    }

    public static TestPassenger CreateTestPassenger(bool isIranian = true)
    {
        var passenger = new TestPassenger
        {
            FirstNameEn = _faker.Name.FirstName(),
            LastNameEn = _faker.Name.LastName(),
            FirstNameFa = _faker.Name.FirstName(),
            LastNameFa = _faker.Name.LastName(),
            BirthDate = _faker.Date.Between(DateTime.Now.AddYears(-70), DateTime.Now.AddYears(-2)),
            Gender = _faker.Random.Number(1, 2),
            IsIranian = isIranian
        };

        if (isIranian)
        {
            passenger.NationalCode = GenerateTestNationalCode();
        }
        else
        {
            passenger.PassportNo = _faker.Random.AlphaNumeric(8).ToUpper();
        }

        return passenger;
    }

    public static List<TestPassenger> CreateTestPassengers(int count, bool isIranian = true)
    {
        var passengers = new List<TestPassenger>();
        for (int i = 0; i < count; i++)
        {
            passengers.Add(CreateTestPassenger(isIranian));
        }
        return passengers;
    }

    public static async Task WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan? interval = null)
    {
        interval ??= TimeSpan.FromSeconds(1);
        var endTime = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < endTime)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(interval.Value);
        }

        throw new TimeoutException($"Condition was not met within {timeout.TotalSeconds} seconds");
    }

    public static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, TimeSpan? interval = null)
    {
        await WaitForConditionAsync(() => Task.FromResult(condition()), timeout, interval);
    }

    public static string GenerateOrderReference()
    {
        return $"TEST-ORDER-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{_faker.Random.Number(1000, 9999)}";
    }

    public static string GenerateTransactionDescription(string operation)
    {
        return $"Test {operation} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    public static decimal GenerateTestAmount(decimal min = 10000, decimal max = 1000000)
    {
        return _faker.Random.Decimal(min, max);
    }

    private static int CalculateLuhnCheckDigit(string number)
    {
        var sum = 0;
        var isEven = true;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';

            if (isEven)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            isEven = !isEven;
        }

        return (10 - (sum % 10)) % 10;
    }

    private static int CalculateShabaCheckDigits(string shaba)
    {
        // Simplified SHABA check digit calculation
        // In real implementation, should use proper IBAN mod-97 algorithm
        var hash = shaba.GetHashCode();
        return Math.Abs(hash % 97) + 1;
    }

    public static string MaskSensitiveData(string data, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= visibleChars)
            return data;

        return "****" + data[^visibleChars..];
    }

    public static bool IsValidIranianMobile(string mobile)
    {
        return !string.IsNullOrEmpty(mobile) &&
               mobile.Length == 11 &&
               mobile.StartsWith("09") &&
               mobile.All(char.IsDigit);
    }

    public static bool IsValidIranianNationalCode(string nationalCode)
    {
        if (string.IsNullOrEmpty(nationalCode) || nationalCode.Length != 10 || !nationalCode.All(char.IsDigit))
            return false;

        var digits = nationalCode.Select(c => int.Parse(c.ToString())).ToArray();
        var checkDigit = digits[9];
        var sum = 0;

        for (int i = 0; i < 9; i++)
        {
            sum += digits[i] * (10 - i);
        }

        var remainder = sum % 11;
        return (remainder < 2 && checkDigit == remainder) ||
               (remainder >= 2 && checkDigit == 11 - remainder);
    }
}