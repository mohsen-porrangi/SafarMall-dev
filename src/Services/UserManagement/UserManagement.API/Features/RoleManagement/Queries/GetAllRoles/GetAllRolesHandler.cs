using UserManagement.API.Infrastructure.Data;

namespace UserManagement.API.Features.RoleManagement.Queries.GetAllRoles;

internal sealed class GetAllRolesQueryHandler(UserDbContext db)
    : IQueryHandler<GetAllRolesQuery, GetAllRolesResult>
{
    public async Task<GetAllRolesResult> Handle(GetAllRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .Select(r => new RoleDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);

        return new GetAllRolesResult(roles);
    }
}
