//namespace UserManagement.API.Endpoints.Auth.Password.ResetPassword;

using UserManagement.API.Features.Authentication.Commands.ResetPassword;

public class ResetPasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/reset-password", async (
            ResetPasswordCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(command, ct);
            return Results.NoContent();
        })
            .WithName("UserResetPassword")
            .WithSummary("User Reset Password")
            .WithDescription("بازیابی رمز عبور با استفاده از شماره موبایل و کد تأیید")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithTags("Auth")
            .AllowAnonymous();
    }
}