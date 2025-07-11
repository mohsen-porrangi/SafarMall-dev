namespace UserManagement.API.Features.RoleManagement.Commands.CreateRole;

public record CreateRoleCommand(string Name) : ICommand<int>;
