using UserManagement.API.Features.Authentication.Commands.Login;

namespace UserManagement.API.Features.Authentication.Commands.VerifyRegistrationOtp;

public record VerifyRegisterOtpCommand(string Mobile, string Otp) : ICommand<LoginResult>;
