namespace UserManagement.API.Features.RoleManagement.Commands.UnassignRole;
public record UnassignRoleFromUserCommand(Guid UserId, int RoleId) : ICommand;

