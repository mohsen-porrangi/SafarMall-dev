namespace UserManagement.API.Features.Internal.Queries.Users.CheckUserByMobile
{

    public record CheckUserByMobileQuery(string Mobile) : IQuery<CheckUserByMobileResponse>;

    public record CheckUserByMobileResponse(bool Exists);
}
