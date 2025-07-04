namespace UserManagement.API.Features.PermissionManagement.Commands.AssignPermission;

public record AssignPermissionCommand(int RoleId, int PermissionId) : ICommand;
