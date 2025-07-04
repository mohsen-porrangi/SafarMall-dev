namespace UserManagement.API.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : ICommand<(string AccessToken, string RefreshToken)>;

