using BuildingBlocks.Contracts;
using BuildingBlocks.Exceptions;

namespace Train.API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetCurrentUserId()
    {
        var userId = httpContextAccessor.HttpContext?.Items["UserId"] as Guid?;

        return userId ?? throw new UnauthorizedDomainException("کاربر احراز هویت نشده است");
    }
    public Guid GetCurrentMasterIdentityId()
    {
        var masterId = httpContextAccessor.HttpContext?.Items["CurrentMasterIdentityId"] as Guid?;

        return masterId ?? throw new UnauthorizedDomainException("کاربر احراز هویت نشده است");
    }

    public Guid GetCurrentUserAccountId()
    {
        throw new NotImplementedException();
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
