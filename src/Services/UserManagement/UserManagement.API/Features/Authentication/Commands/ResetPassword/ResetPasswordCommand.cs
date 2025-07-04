namespace UserManagement.API.Features.Authentication.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Mobile,
    string? Otp = null,
    string? NewPassword = null
) : ICommand;