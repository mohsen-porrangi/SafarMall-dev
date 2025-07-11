using BuildingBlocks.Enums;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Models;
using PaymentGateway.API.Providers.Sandbox;
using PaymentGateway.API.Providers.ZarinPal;
using PaymentGateway.API.Providers.Zibal;

namespace PaymentGateway.API.Providers;


/// <summary>
/// پیاده‌سازی کارخانه درگاه پرداخت
/// </summary>
public class PaymentGatewayFactory(IServiceProvider serviceProvider) : IPaymentGatewayFactory
{
    public IPaymentProvider GetProvider(PaymentGatewayType gatewayType)
    {
        return gatewayType switch
        {
            PaymentGatewayType.ZarinPal => serviceProvider.GetRequiredService<ZarinPalProvider>(),
            PaymentGatewayType.Zibal => serviceProvider.GetRequiredService<ZibalProvider>(),
            PaymentGatewayType.Sandbox => serviceProvider.GetRequiredService<SandboxProvider>(),
            _ => throw new NotSupportedException($"درگاه {gatewayType} پشتیبانی نمی‌شود")
        };
    }
    public bool IsSupported(PaymentGatewayType gatewayType) =>
        Enum.IsDefined<PaymentGatewayType>(gatewayType);

    public IEnumerable<PaymentGatewayType> GetSupportedGateways() =>
        [PaymentGatewayType.ZarinPal, PaymentGatewayType.Zibal, PaymentGatewayType.Sandbox];
}