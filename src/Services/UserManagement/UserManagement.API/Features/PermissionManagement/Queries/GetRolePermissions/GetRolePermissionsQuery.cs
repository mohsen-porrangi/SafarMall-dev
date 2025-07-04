﻿namespace UserManagement.API.Features.PermissionManagement.Queries.GetRolePermissions;

public record GetRolePermissionsQuery(int RoleId) : IQuery<GetRolePermissionsResult>;

public record GetRolePermissionsResult(IEnumerable<PermissionDto> Permissions);

public record PermissionDto(int Id, string Module, string Action, string Code, string Description);

