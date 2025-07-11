using Microsoft.Extensions.Configuration;

namespace SafarMall.IntegrationTests.Configuration;

public static class TestConfiguration
{
    private static IConfiguration? _configuration;

    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.test.json", optional: false)
                    .AddEnvironmentVariables();

                _configuration = builder.Build();
            }
            return _configuration;
        }
    }

    public static class ServiceUrls
    {
        public static string UserManagement => Configuration["Services:UserManagement:BaseUrl"] ?? "https://localhost:7072";
        public static string Wallet => Configuration["Services:Wallet:BaseUrl"] ?? "https://localhost:7240";
        public static string Order => Configuration["Services:Order:BaseUrl"] ?? "https://localhost:60102";
        public static string PaymentGateway => Configuration["Services:PaymentGateway:BaseUrl"] ?? "https://localhost:7001";
    }

    public static class TestData
    {
        public static string DefaultOtp => Configuration["TestData:DefaultOtp"] ?? "111111";
        public static string TestPassword => Configuration["TestData:TestPassword"] ?? "Test@123456";
        public static string TestBankName => Configuration["TestData:TestBankName"] ?? "بانک ملت";
        public static string TestAccountNumber => Configuration["TestData:TestAccountNumber"] ?? "1234567890123456";
        public static string TestCardNumber => Configuration["TestData:TestCardNumber"] ?? "6104337650001234";
        public static string TestShabaNumber => Configuration["TestData:TestShabaNumber"] ?? "IR123456789012345678901234";
    }

    public static class Timeouts
    {
        public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(30);
        public static TimeSpan LongTimeout => TimeSpan.FromMinutes(2);
        public static TimeSpan ShortTimeout => TimeSpan.FromSeconds(10);
    }
}