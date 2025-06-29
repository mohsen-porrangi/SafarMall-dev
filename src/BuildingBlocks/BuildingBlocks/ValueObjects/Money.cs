using BuildingBlocks.Enums;

namespace BuildingBlocks.ValueObjects;

/// <summary>
/// Value Object برای نمایش مبلغ با ارز
/// </summary>
public record Money
{
    public decimal Value { get; }
    public CurrencyCode Currency { get; }

    public Money(decimal value, CurrencyCode currency)
    {
        if (value < 0)
            throw new ArgumentException("مبلغ نمی‌تواند منفی باشد", nameof(value));

        ValidateCurrencyPrecision(value, currency);

        Value = value;
        Currency = currency;
    }

    /// <summary>
    /// Factory method to create Money with validation
    /// </summary>
    public static Money Create(decimal value, CurrencyCode currency)
    {
        return new Money(value, currency);
    }

    /// <summary>
    /// ایجاد Money با مقدار صفر
    /// </summary>
    public static Money Zero(CurrencyCode currency) => new(0, currency);

    /// <summary>
    /// ایجاد Money با ریال
    /// </summary>
    public static Money FromIrr(decimal amount) => new(amount, CurrencyCode.IRR);

    /// <summary>
    /// ایجاد Money با دلار (آینده)
    /// </summary>
    public static Money FromUsd(decimal amount) => new(amount, CurrencyCode.USD);

    /// <summary>
    /// ایجاد Money با یورو (آینده)
    /// </summary>
    public static Money FromEur(decimal amount) => new(amount, CurrencyCode.EUR);

    /// <summary>
    /// جمع دو مبلغ با ارز یکسان
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"نمی‌توان {Currency} را با {other.Currency} جمع کرد");

        return new Money(Value + other.Value, Currency);
    }

    /// <summary>
    /// تفریق دو مبلغ با ارز یکسان
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"نمی‌توان {other.Currency} را از {Currency} کم کرد");

        return new Money(Value - other.Value, Currency);
    }

    /// <summary>
    /// مقایسه بزرگتر بودن
    /// </summary>
    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"نمی‌توان {Currency} را با {other.Currency} مقایسه کرد");

        return Value > other.Value;
    }

    /// <summary>
    /// بررسی کفایت مبلغ
    /// </summary>
    public bool IsSufficientFor(Money requiredAmount)
    {
        return Currency == requiredAmount.Currency && Value >= requiredAmount.Value;
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Value * factor, Currency);
    }

    /// <summary>
    /// اعتبارسنجی دقت ارز
    /// </summary>
    private static void ValidateCurrencyPrecision(decimal value, CurrencyCode currency)
    {
        var allowedDecimalPlaces = currency switch
        {
            CurrencyCode.IRR => 0, // ریال فقط عدد صحیح
            _ => 2 // سایر ارزها حداکثر 2 رقم اعشار
        };

        if (Math.Round(value, allowedDecimalPlaces) != value)
            throw new ArgumentException($"ارز {currency} حداکثر {allowedDecimalPlaces} رقم اعشار مجاز است", nameof(value));
    }

    public override string ToString() => $"{Value:N} {Currency}";
}




//namespace Order.Domain.ValueObjects;

//public record Money
//{
//    public decimal Amount { get; }
//    public string Currency { get; }

//    public Money(decimal amount, string currency = "IRR")
//    {
//        if (amount < 0)
//            throw new ArgumentException("Amount cannot be negative", nameof(amount));

//        if (string.IsNullOrWhiteSpace(currency))
//            throw new ArgumentException("Currency cannot be empty", nameof(currency));

//        Amount = amount;
//        Currency = currency;
//    }

//    public Money Add(Money other)
//    {
//        if (Currency != other.Currency)
//            throw new InvalidOperationException($"Cannot add money with different currencies: {Currency} and {other.Currency}");

//        return new Money(Amount + other.Amount, Currency);
//    }

//    public Money Subtract(Money other)
//    {
//        if (Currency != other.Currency)
//            throw new InvalidOperationException($"Cannot subtract money with different currencies: {Currency} and {other.Currency}");

//        return new Money(Amount - other.Amount, Currency);
//    }

//    public Money Multiply(decimal factor)
//    {
//        return new Money(Amount * factor, Currency);
//    }

//    public static Money Zero(string currency = "IRR") => new(0, currency);

//    public override string ToString() => $"{Amount:N0} {Currency}";
//}