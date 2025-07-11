namespace UserManagement.API.Features.Authentication.Queries.CheckUserByMobile
{

    public record CheckUserByMobileQuery(string Mobile) : IQuery<CheckUserByMobileResponse>;

    public record CheckUserByMobileResponse(bool Exists);
}
