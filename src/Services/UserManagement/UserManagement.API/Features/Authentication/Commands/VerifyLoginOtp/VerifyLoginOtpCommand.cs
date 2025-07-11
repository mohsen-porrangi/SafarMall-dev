using UserManagement.API.Features.Authentication.Commands.Login;

namespace UserManagement.API.Features.Authentication.Commands.VerifyLoginOtp;

public record VerifyLoginOtpCommand(
    string Mobile,
    string Otp
) : ICommand<LoginResult>;