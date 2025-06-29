using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Providers;


/// <summary>
/// پیاده‌سازی کارخانه درگاه پرداخت
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PaymentGatewayType, Type> _providerTypes;

    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // ثبت انواع ارائه‌دهنده‌ها
        _providerTypes = new Dictionary<PaymentGatewayType, Type>
        {
            { PaymentGatewayType.ZarinPal, typeof(ZarinPal.ZarinPalProvider) },
            { PaymentGatewayType.Zibal, typeof(Zibal.ZibalProvider) },
            { PaymentGatewayType.Sandbox, typeof(Sandbox.SandboxProvider) }
        };
    }

    public IPaymentProvider GetProvider(PaymentGatewayType gatewayType)
    {
        if (!_providerTypes.TryGetValue(gatewayType, out var providerType))
        {
            throw new NotSupportedException($"درگاه پرداخت {gatewayType} پشتیبانی نمی‌شود");
        }

        var provider = _serviceProvider.GetService(providerType) as IPaymentProvider;

        if (provider == null)
        {
            throw new InvalidOperationException($"ارائه‌دهنده {gatewayType} یافت نشد");
        }

        return provider;
    }

    public bool IsSupported(PaymentGatewayType gatewayType)
    {
        return _providerTypes.ContainsKey(gatewayType);
    }

    public IEnumerable<PaymentGatewayType> GetSupportedGateways()
    {
        return _providerTypes.Keys;
    }
}