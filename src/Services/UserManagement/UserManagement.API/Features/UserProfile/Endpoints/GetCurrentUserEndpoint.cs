using System.Security.Claims;
using UserManagement.API.Common.Extensions;
using UserManagement.API.Features.UserProfile.Queries.GetCurrentUser;

namespace UserManagement.API.Features.UserProfile.Endpoints
{
    public class GetCurrentUserEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/current", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken ct) =>
            {
                var identityId = user.GetIdentityId();
                var result = await sender.Send(new GetCurrentUserQuery(identityId), ct);
                return Results.Ok(result);
            })
                .WithName("GetUserProfile")
                .WithSummary("Get User Profile")
                .WithDescription("دریافت اطلاعات پروفایل کاربر جاری")
                .Produces<GetCurrentUserResult>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithTags("Profile")
                .RequireAuthorization();
        }
    }
}
