using Bogus;
using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Models;

namespace SafarMall.IntegrationTests.TestFixtures;

public static class TestDataGenerator
{
    private static readonly Faker _faker = new("fa");
    private static readonly Random _random = new();

    #region User Generation

    public static TestUser GenerateTestUser()
    {
        return new TestUser
        {
            Id = Guid.NewGuid(),
            Mobile = GenerateIranianMobile(),
            Password = TestConfiguration.TestData.TestPassword,
            Name = _faker.Name.FirstName(),
            Family = _faker.Name.LastName(),
            NationalCode = GenerateValidNationalCode()
        };
    }

    public static List<TestUser> GenerateTestUsers(int count)
    {
        var users = new List<TestUser>();
        for (int i = 0; i < count; i++)
        {
            users.Add(GenerateTestUser());
        }
        return users;
    }

    #endregion

    #region Passenger Generation

    public static TestPassenger GenerateTestPassenger(bool isIranian = true, int? ageYears = null)
    {
        var birthDate = ageYears.HasValue
            ? DateTime.Now.AddYears(-ageYears.Value)
            : _faker.Date.Between(DateTime.Now.AddYears(-70), DateTime.Now.AddYears(-2));

        var passenger = new TestPassenger
        {
            FirstNameEn = GenerateEnglishFirstName(),
            LastNameEn = GenerateEnglishLastName(),
            FirstNameFa = _faker.Name.FirstName(),
            LastNameFa = _faker.Name.LastName(),
            BirthDate = birthDate,
            Gender = _random.Next(1, 3), // 1 = Male, 2 = Female
            IsIranian = isIranian
        };

        if (isIranian)
        {
            passenger.NationalCode = GenerateValidNationalCode();
        }
        else
        {
            passenger.PassportNo = GeneratePassportNo();
        }

        return passenger;
    }

    public static List<TestPassenger> GenerateTestPassengers(int count, bool isIranian = true)
    {
        var passengers = new List<TestPassenger>();
        for (int i = 0; i < count; i++)
        {
            passengers.Add(GenerateTestPassenger(isIranian));
        }
        return passengers;
    }

    public static TestPassenger GenerateAdultPassenger(bool isIranian = true)
    {
        return GenerateTestPassenger(isIranian, _random.Next(18, 65));
    }

    public static TestPassenger GenerateChildPassenger(bool isIranian = true)
    {
        return GenerateTestPassenger(isIranian, _random.Next(2, 12));
    }

    public static TestPassenger GenerateInfantPassenger(bool isIranian = true)
    {
        return GenerateTestPassenger(isIranian, _random.Next(0, 2));
    }

    #endregion

    #region Order Generation

    public static object GenerateOrderRequest(List<TestPassenger> passengers, string serviceType = "Train", bool hasReturn = false)
    {
        var cities = GetTestCities();
        var sourceCity = cities[_random.Next(cities.Count)];
        var destinationCity = cities.Where(c => c.Code != sourceCity.Code).OrderBy(x => Guid.NewGuid()).First();

        var departureDate = DateTime.Now.AddDays(_random.Next(1, 30));
        var returnDate = hasReturn ? departureDate.AddDays(_random.Next(1, 14)) : (DateTime?)null;

        return new
        {
            serviceType = serviceType,
            sourceCode = sourceCity.Code,
            destinationCode = destinationCity.Code,
            sourceName = sourceCity.Name,
            destinationName = destinationCity.Name,
            departureDate = departureDate,
            returnDate = returnDate,
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
    }

    #endregion

    #region Bank Account Generation

    public static object GenerateBankAccountRequest(string? accountHolderName = null)
    {
        var banks = GetIranianBanks();
        var selectedBank = banks[_random.Next(banks.Count)];

        return new
        {
            bankName = selectedBank,
            accountNumber = GenerateAccountNumber(),
            cardNumber = GenerateValidCardNumber(),
            shabaNumber = GenerateValidShabaNumber(),
            accountHolderName = accountHolderName ?? $"{_faker.Name.FirstName()} {_faker.Name.LastName()}"
        };
    }

    #endregion

    #region Transaction Generation

    public static decimal GenerateAmount(decimal min = 10000m, decimal max = 5000000m)
    {
        return Math.Round(_faker.Random.Decimal(min, max), 0);
    }

    public static string GenerateTransactionDescription(string operation)
    {
        var descriptions = new Dictionary<string, List<string>>
        {
            ["deposit"] = new List<string>
            {
                "شارژ کیف پول",
                "واریز مستقیم",
                "افزایش موجودی",
                "شارژ آنلاین"
            },
            ["purchase"] = new List<string>
            {
                "خرید بلیط قطار",
                "خرید بلیط پرواز",
                "پرداخت سفارش",
                "خرید آنلاین"
            },
            ["transfer"] = new List<string>
            {
                "انتقال وجه",
                "حواله آنلاین",
                "انتقال بین کاربران",
                "ارسال پول"
            },
            ["refund"] = new List<string>
            {
                "استرداد وجه",
                "بازگشت پول",
                "لغو خرید",
                "عودت مبلغ"
            }
        };

        if (descriptions.TryGetValue(operation.ToLower(), out var operationDescriptions))
        {
            return operationDescriptions[_random.Next(operationDescriptions.Count)];
        }

        return $"تراکنش {operation}";
    }

    #endregion

    #region Validation Helpers

    public static string GenerateIranianMobile()
    {
        var operators = new[] { "091", "099", "090", "093", "094", "095", "096", "097", "098" };
        var selectedOperator = operators[_random.Next(operators.Length)];
        var remainingDigits = _random.Next(10000000, 99999999);
        return $"{selectedOperator}{remainingDigits}";
    }

    public static string GenerateValidNationalCode()
    {
        var digits = new int[10];

        // Generate first 9 digits randomly
        for (int i = 0; i < 9; i++)
        {
            digits[i] = _random.Next(0, 10);
        }

        // Calculate check digit using Iranian national code algorithm
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += digits[i] * (10 - i);
        }

        var remainder = sum % 11;
        digits[9] = remainder < 2 ? remainder : 11 - remainder;

        return string.Join("", digits);
    }

    public static string GenerateValidCardNumber()
    {
        // Common Iranian bank prefixes
        var bankPrefixes = new[] { "6104", "6037", "5041", "6276", "5029", "6280", "6362", "5022" };
        var selectedPrefix = bankPrefixes[_random.Next(bankPrefixes.Length)];

        // Generate middle digits
        var middleDigits = _random.Next(100000, 999999).ToString();
        var lastDigits = _random.Next(1000, 9999).ToString();

        var cardNumber = selectedPrefix + middleDigits + lastDigits;

        // Calculate and append Luhn check digit
        var checkDigit = CalculateLuhnCheckDigit(cardNumber);
        return cardNumber + checkDigit;
    }

    public static string GenerateValidShabaNumber()
    {
        // Generate bank code (3 digits)
        var bankCode = _random.Next(100, 999).ToString();

        // Generate account number (13 digits)
        var accountNumber = _random.Next(1000000, 9999999).ToString().PadLeft(13, '0');

        // Simple check digit calculation (not real IBAN algorithm)
        var checkDigits = (_random.Next(10, 99)).ToString();

        return $"IR{checkDigits}{bankCode}{accountNumber}";
    }

    public static string GenerateAccountNumber()
    {
        return _random.Next(1000000000, int.MaxValue).ToString();
    }

    public static string GeneratePassportNo()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var PassportNo = "";

        // Add 1-2 letters
        PassportNo += letters[_random.Next(letters.Length)];
        if (_random.Next(2) == 0)
        {
            PassportNo += letters[_random.Next(letters.Length)];
        }

        // Add 6-8 digits
        var digitCount = _random.Next(6, 9);
        for (int i = 0; i < digitCount; i++)
        {
            PassportNo += _random.Next(0, 10);
        }

        return PassportNo;
    }

    public static string GenerateEnglishFirstName()
    {
        var names = new[]
        {
            "John", "Jane", "Michael", "Sarah", "David", "Emma", "Chris", "Lisa",
            "Robert", "Anna", "James", "Maria", "William", "Jennifer", "Richard",
            "Susan", "Thomas", "Jessica", "Daniel", "Ashley", "Matthew", "Amanda"
        };
        return names[_random.Next(names.Length)];
    }

    public static string GenerateEnglishLastName()
    {
        var surnames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
            "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez",
            "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin"
        };
        return surnames[_random.Next(surnames.Length)];
    }

    #endregion

    #region Reference Data

    private static List<(int Code, string Name)> GetTestCities()
    {
        return new List<(int Code, string Name)>
        {
            (1, "تهران"),
            (2, "اصفهان"),
            (3, "مشهد"),
            (4, "شیراز"),
            (5, "تبریز"),
            (6, "اهواز"),
            (7, "کرج"),
            (8, "قم"),
            (9, "کرمانشاه"),
            (10, "ارومیه"),
            (11, "رشت"),
            (12, "زاهدان"),
            (13, "همدان"),
            (14, "کرمان"),
            (15, "یزد")
        };
    }

    private static List<string> GetIranianBanks()
    {
        return new List<string>
        {
            "بانک ملی ایران",
            "بانک ملت",
            "بانک صادرات ایران",
            "بانک تجارت",
            "بانک کشاورزی",
            "بانک رفاه کارگران",
            "بانک صنعت و معدن",
            "بانک پاسارگاد",
            "بانک پارسیان",
            "بانک کارآفرین",
            "بانک سامان",
            "بانک سرمایه",
            "بانک دی",
            "بانک ایران زمین",
            "بانک مهر اقتصاد"
        };
    }

    public static List<string> GetServiceTypes()
    {
        return new List<string> { "Train", "DomesticFlight", "InternationalFlight" };
    }

    #endregion

    #region Utility Methods

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

    public static string GenerateRandomString(int length, bool includeNumbers = true)
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (includeNumbers)
            chars += "0123456789";

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static DateTime GenerateRandomDate(DateTime? minDate = null, DateTime? maxDate = null)
    {
        minDate ??= DateTime.Now.AddYears(-1);
        maxDate ??= DateTime.Now.AddYears(1);

        var range = (maxDate.Value - minDate.Value).Days;
        return minDate.Value.AddDays(_random.Next(range));
    }

    #endregion
}