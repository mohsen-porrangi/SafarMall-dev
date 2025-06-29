namespace UserManagement.API.Endpoints.Internal.Users.GetIds
{
    public class GetAllUsersQueryHandler(IUserRepository repository) : IQueryHandler<GetAllUsersIdQuery, GetAllUsersIdResult>
    {
        public async Task<GetAllUsersIdResult> Handle(GetAllUsersIdQuery request, CancellationToken cancellationToken)
        {
            var users = await repository.GetAllAsync(false, cancellationToken);
            var result = users.Select(user => new UserIdDto(
               user.Id
           ));

            return new GetAllUsersIdResult(result);
        }
    }
}
