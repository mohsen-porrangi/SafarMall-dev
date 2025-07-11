using BuildingBlocks.Enums;

namespace UserManagement.API.Features.UserProfile.Commands.EditCurrentUser
{
    public record EditCurrentUserCommand(
        Guid UserId,
        string Name,
        string Family,
        string? NationalCode,
        Gender? Gender,
        DateTime BirthDate,
        // فیلدهای اختیاری برای تغییر رمز عبور
        string? CurrentPassword = null,
        string? NewPassword = null
    ) : ICommand;
}