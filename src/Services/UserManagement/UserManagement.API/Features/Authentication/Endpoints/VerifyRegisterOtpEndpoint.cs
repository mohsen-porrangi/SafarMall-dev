using UserManagement.API.Features.Authentication.Commands.Login;
using UserManagement.API.Features.Authentication.Commands.VerifyRegistrationOtp;

namespace UserManagement.API.Features.Authentication.Endpoints;

public class VerifyRegisterOtpEndpointRemove : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/register/verify-otp", async (
            VerifyRegisterOtpCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
          .WithTags("Auth")
          .WithSummary("Verify OTP Register")
          .WithName("UserRegisterVerifyOtp")
          .WithDescription("تأیید کد یکبار مصرف برای فعال‌سازی حساب کاربری")
          .Produces<LoginResult>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status401Unauthorized)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .AllowAnonymous();
    }
}
