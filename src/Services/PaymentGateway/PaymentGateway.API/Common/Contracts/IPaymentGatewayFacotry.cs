using BuildingBlocks.Enums;
using PaymentGateway.API.Models;
using PaymentGateway.API.Providers;

namespace PaymentGateway.API.Common.Contracts;

/// <summary>
/// کارخانه ایجاد ارائه‌دهنده درگاه پرداخت
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// دریافت ارائه‌دهنده بر اساس نوع درگاه
    /// </summary>
    IPaymentProvider GetProvider(PaymentGatewayType gatewayType);

    /// <summary>
    /// بررسی پشتیبانی از نوع درگاه
    /// </summary>
    bool IsSupported(PaymentGatewayType gatewayType);

    /// <summary>
    /// دریافت لیست درگاه‌های پشتیبانی شده
    /// </summary>
    IEnumerable<PaymentGatewayType> GetSupportedGateways();
}

