namespace PaymentGateway.API.Models;

/// <summary>
/// نوع درگاه پرداخت
/// </summary>
public enum PaymentGatewayType
{
    /// <summary>
    /// زرین پال
    /// </summary>
    ZarinPal = 1,

    /// <summary>
    /// زیبال
    /// </summary>
    Zibal = 2,

    /// <summary>
    /// محیط تست
    /// </summary>
    Sandbox = 99
}