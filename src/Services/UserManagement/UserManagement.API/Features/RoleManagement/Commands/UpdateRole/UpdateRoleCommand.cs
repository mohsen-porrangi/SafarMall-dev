namespace UserManagement.API.Features.RoleManagement.Commands.UpdateRole;

public record UpdateRoleCommand(int Id, string Name) : ICommand;
