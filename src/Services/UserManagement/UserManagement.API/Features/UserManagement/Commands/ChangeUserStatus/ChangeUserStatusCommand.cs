namespace UserManagement.API.Features.UserManagement.Commands.ChangeUserStatus;

public record ChangeUserStatusCommand(Guid Id, bool IsActive) : ICommand;
public record ChangeUserStatusRequest(bool IsActive);