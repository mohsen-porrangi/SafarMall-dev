namespace UserManagement.API.Features.UserManagement.Commands.DeleteUser
{
    public record DeleteUserCommand(Guid Id) : ICommand;
}
