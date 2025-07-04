namespace UserManagement.API.Features.Authentication.Commands.VerifyOtpPassword;


public record VerifyResetPasswordOtpCommand(Guid ResetToken, string Otp) : ICommand<bool>;
