namespace UserManagement.API.Features.Authentication.Commands.Login;

public record LoginCommand(
    string Mobile,
    string? Password = null
) : ICommand<LoginResult>;

public record LoginResult(
    string? Token = null,
    string? Message = null,
    string? NextStep = null
);