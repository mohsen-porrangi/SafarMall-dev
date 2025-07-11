namespace UserManagement.API.Features.PermissionManagement.Commands.UnassignPermission;

public record UnassignPermissionCommand(int RoleId, int PermissionId) : ICommand;
