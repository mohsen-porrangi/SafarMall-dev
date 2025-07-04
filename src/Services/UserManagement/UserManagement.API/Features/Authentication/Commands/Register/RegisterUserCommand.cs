namespace UserManagement.API.Features.Authentication.Commands.Register;

public record RegisterUserCommand(
    //   string Email,
    string Mobile,
    string Password
) : ICommand;