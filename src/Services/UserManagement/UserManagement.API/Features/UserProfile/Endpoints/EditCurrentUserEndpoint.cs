using System.Security.Claims;
using UserManagement.API.Common.Extensions;
using UserManagement.API.Features.UserProfile.Commands.EditCurrentUser;

namespace UserManagement.API.Features.UserProfile.Endpoints
{
    public class EditCurrentUserEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("api/users/current", async (
                EditCurrentUserCommand command,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken ct) =>
            {
                var identityId = user.GetIdentityId();
                var fullCommand = command with { UserId = identityId };

                await sender.Send(fullCommand, ct);
                return Results.NoContent();
            })
                .WithName("EditUserProfile")
                .WithSummary("Edit User Profile")
                .WithDescription("ویرایش اطلاعات پروفایل کاربر و تغییر رمز عبور")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithTags("Profile")
                .RequireAuthorization();
        }
    }
}