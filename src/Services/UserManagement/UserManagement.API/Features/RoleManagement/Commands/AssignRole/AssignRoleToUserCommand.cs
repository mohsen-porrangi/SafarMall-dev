namespace UserManagement.API.Features.RoleManagement.Commands.AssignRole;

public record AssignRoleToUserCommand(Guid UserId, int RoleId) : ICommand;