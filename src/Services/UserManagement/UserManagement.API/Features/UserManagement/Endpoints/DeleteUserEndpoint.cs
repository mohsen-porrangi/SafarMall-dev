using UserManagement.API.Features.UserManagement.Commands.DeleteUser;

namespace UserManagement.API.Features.UserManagement.Endpoints
{
    public class DeleteUserEndpoint : ICarterModule
    {
        //  [RequirePermission(UserPermissions.Delete)]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/users/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new DeleteUserCommand(id), ct);
                return Results.NoContent();
            })
             .WithName("RemoveUser")
             .WithSummary("Remove User")
             .WithDescription("حذف کاربر")
             .Produces(StatusCodes.Status204NoContent)
             .Produces(StatusCodes.Status401Unauthorized)
             .Produces(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .WithTags("Users")
             .RequireAuthorization("Admin");
        }
    }
}
