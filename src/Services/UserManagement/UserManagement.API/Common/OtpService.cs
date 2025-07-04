using BuildingBlocks.Services;
using Microsoft.Extensions.Logging;

namespace UserManagement.API.Common;

public class OtpService(HttpClient httpClient, ILogger<OtpService> logger) : BaseHttpClient(httpClient, logger), IOtpService
{
    // به عنوان یک نمونه ساده موقت
    private static readonly Dictionary<string, string> OtpStore = new();

    public async Task SendOtpAsync(string phoneNumber)
    {
          await GetAsync($"http://185.129.170.40:8080/ApiGateway/api/sms-service/SMS/SendOTP?phoneNumber={phoneNumber}");
    }

    public async Task<bool> ValidateOtpAsync(string mobile, string otp)
    {
        //return true;
        try { 
        await PostAsync("http://185.129.170.40:8080/ApiGateway/api/sms-service/SMS/VerifyOTP", new VerifyDTO(mobile,otp));        
        return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public record VerifyDTO(string phoneNumber, string code);
}
