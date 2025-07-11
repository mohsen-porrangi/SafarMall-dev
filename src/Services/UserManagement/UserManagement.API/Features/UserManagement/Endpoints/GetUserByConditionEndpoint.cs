using UserManagement.API.Features.UserManagement.Queries.GetUserByCondition;

namespace UserManagement.API.Features.UserManagement.Endpoints
{
    public class GetUserByConditionEndpoint : ICarterModule
    {
        //   [RequirePermission(UserPermissions.Read)]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/search", async (
                ISender sender,
                CancellationToken ct,
                string? name = null,
                string? family = null,
                string? mobile = null,
                bool? isActive = null,
                string? nationalCode = null,
                int page = 1,
                int pageSize = 10) =>
            {
                var query = new GetUserByConditionQuery(
                    name, family, mobile, isActive, nationalCode, page, pageSize);
                var result = await sender.Send(query, ct);
                return Results.Ok(result);
            })
                .WithName("FilterUsers")
                .WithSummary("Search User")
                .WithDescription("جستجوی کاربران بر اساس شرایط مختلف")
                .Produces<GetUserByConditionResult>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .WithTags("Users")
                .RequireAuthorization("Admin");
        }
    }
}