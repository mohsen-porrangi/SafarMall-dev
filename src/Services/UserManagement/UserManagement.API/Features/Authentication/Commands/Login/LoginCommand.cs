namespace UserManagement.API.Features.Authentication.Commands.Login;

public record LoginCommand(
    //string? Email,
    string? Mobile,
    string? Password,
    string? Otp
) : ICommand<LoginResult>;

public record LoginResult(
    bool Success,
    string? Token = null,
    string? Message = null,
    string? NextStep = null
);