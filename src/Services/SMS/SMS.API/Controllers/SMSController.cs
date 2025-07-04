using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMS.API.Services;

namespace SMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SMSController(SmsService smsService) : ControllerBase
    {       
        [HttpGet("SendOTP")]
        public async Task<IActionResult> SendOTPAsync([FromQuery] string phoneNumber)
        {
            await smsService.SendOTPAsync(phoneNumber);
            return Ok();
        }

        [HttpPost("VerifyOTP")]
        public IActionResult VerifyOTP(VerifyDTO request)
        {
            smsService.CheckVarifactionCode(request.code, request.phoneNumber);
            return Ok();
        }
    }
    public record VerifyDTO(string phoneNumber, string code);
}
