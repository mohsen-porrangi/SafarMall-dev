// Path: src/Services/UserManagement/UserManagement.API/Features/Authentication/Commands/Login/LoginCommandHandler.cs

using BuildingBlocks.Exceptions;

namespace UserManagement.API.Features.Authentication.Commands.Login;

internal sealed class LoginCommandHandler(
    IUnitOfWork uow,
    ITokenService tokenService,
    IOtpService otpService
) : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // حالت 1: فقط موبایل - بررسی کاربر
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            var exists = await uow.Users.UserExistsByMobileAsync(request.Mobile);

            if (exists)
            {
                // کاربر وجود دارد، OTP ارسال می‌کنیم
                await otpService.SendOtpAsync(request.Mobile);
                return new LoginResult(
                    Message: "کد تأیید به شماره موبایل شما ارسال شد",
                    NextStep: "enter-otp"
                );
            }
            else
            {
                // کاربر وجود ندارد
                return new LoginResult(
                    Message: "شما هنوز ثبت‌نام نکرده‌اید",
                    NextStep: "register"
                );
            }
        }

        // حالت 2: موبایل + پسورد - ورود مستقیم
        var identity = await uow.Users.GetIdentityByMobileAsync(request.Mobile);
        if (identity is null || !BCrypt.Net.BCrypt.Verify(request.Password, identity.PasswordHash))
            throw new UnauthorizedDomainException("اطلاعات ورود نامعتبر است");

        var user = await uow.Users.GetUserByIdentityIdAsync(identity.Id)
            ?? throw new NotFoundException("کاربر یافت نشد");

        if (!user.MasterIdentity.IsActive)
            throw new ForbiddenDomainException("حساب کاربری غیرفعال است");

        var permissions = await uow.Users.GetUserPermissionsAsync(user.Id);
        var token = tokenService.GenerateToken(user, permissions);

        return new LoginResult(Token: token);
    }
}