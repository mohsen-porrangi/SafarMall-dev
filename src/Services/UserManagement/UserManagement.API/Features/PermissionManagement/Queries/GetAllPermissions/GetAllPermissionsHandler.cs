using UserManagement.API.Infrastructure.Data;

namespace UserManagement.API.Features.PermissionManagement.Queries.GetAllPermissions;

internal sealed class GetAllPermissionsQueryHandler(UserDbContext db)
    : IQueryHandler<GetAllPermissionsQuery, GetAllPermissionsResult>
{
    public async Task<GetAllPermissionsResult> Handle(GetAllPermissionsQuery query, CancellationToken cancellationToken)
    {
        var list = await db.Permissions
            .Select(p => new PermissionDto(
                p.Id,
                p.Module,
                p.Action,
                p.Code,
                p.Description
            ))
            .ToListAsync(cancellationToken);

        return new GetAllPermissionsResult(list);
    }
}
