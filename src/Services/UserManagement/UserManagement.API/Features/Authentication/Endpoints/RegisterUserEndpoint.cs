using UserManagement.API.Features.Authentication.Commands.Register;

namespace UserManagement.API.Features.Authentication.Endpoints;

public class RegisterUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/register", async (
            RegisterUserCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(command, ct);
            return Results.NoContent();
        })
            .WithTags("Auth")
            .WithSummary("Register User")
            .WithName("UserRegister")
            .WithDescription("ثبت‌نام کاربر جدید با شماره موبایل و رمز عبور")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }
}
