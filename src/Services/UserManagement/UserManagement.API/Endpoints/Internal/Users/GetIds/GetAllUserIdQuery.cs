namespace UserManagement.API.Endpoints.Internal.Users.GetIds
{
    public record GetAllUsersIdQuery() : IQuery<GetAllUsersIdResult>;
    public record GetAllUsersIdResult(IEnumerable<UserIdDto> Users);
    public record UserIdDto(
      Guid Id
        );
}
