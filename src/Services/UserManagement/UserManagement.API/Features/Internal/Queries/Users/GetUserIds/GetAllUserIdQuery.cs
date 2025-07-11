namespace UserManagement.API.Features.Internal.Queries.Users.GetUserIds
{
    public record GetAllUsersIdQuery() : IQuery<GetAllUsersIdResult>;
    public record GetAllUsersIdResult(IEnumerable<UserIdDto> Users);
    public record UserIdDto(
      Guid Id
        );
}
