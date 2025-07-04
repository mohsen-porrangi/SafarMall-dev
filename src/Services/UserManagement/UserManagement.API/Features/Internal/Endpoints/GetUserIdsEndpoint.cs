using UserManagement.API.Features.Internal.Queries.Users.GetUserIds;

namespace UserManagement.API.Features.Internal.Endpoints
{
    public class GetUserIdsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/internal/users/ids", async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAllUsersIdQuery(), ct);
                return Results.Ok(result);
            }).WithName("ListAllUsersId")
              .WithDescription("دریافت لیست تمام آیدی کاربران")
              .Produces<GetAllUsersIdResult>(StatusCodes.Status200OK)
              .Produces(StatusCodes.Status401Unauthorized)
              .Produces(StatusCodes.Status403Forbidden)
              .WithTags("Users");
        }
    }
}
