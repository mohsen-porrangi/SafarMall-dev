using UserManagement.API.Features.Authentication.Commands.Login;

namespace UserManagement.API.Features.Authentication.Endpoints;

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (
            LoginCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("UserLogin")
        .WithDescription("ورود کاربر با شماره موبایل و رمز عبور یا OTP")
        .Produces<LoginResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithTags("Auth");
    }
}