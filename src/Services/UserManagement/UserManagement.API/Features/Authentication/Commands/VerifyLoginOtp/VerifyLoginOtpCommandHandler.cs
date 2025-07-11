using BuildingBlocks.Exceptions;
using UserManagement.API.Features.Authentication.Commands.Login;

namespace UserManagement.API.Features.Authentication.Commands.VerifyLoginOtp;

internal sealed class VerifyLoginOtpCommandHandler(
    IUnitOfWork uow,
    ITokenService tokenService,
    IOtpService otpService
) : ICommandHandler<VerifyLoginOtpCommand, LoginResult>
{
    public async Task<LoginResult> Handle(VerifyLoginOtpCommand request, CancellationToken cancellationToken)
    {
        // فقط تأیید OTP (بدون ارسال مجدد)
        var isValid = await otpService.ValidateOtpAsync(request.Mobile, request.Otp);
        if (!isValid)
            throw new UnauthorizedDomainException("کد اعتبارسنجی نامعتبر است");

        var identity = await uow.Users.GetIdentityByMobileAsync(request.Mobile)
            ?? throw new NotFoundException("کاربر یافت نشد");

        var user = await uow.Users.GetUserByIdentityIdAsync(identity.Id)
            ?? throw new NotFoundException("کاربر یافت نشد");

        if (!user.MasterIdentity.IsActive)
            throw new ForbiddenDomainException("حساب کاربری غیرفعال است");

        var permissions = await uow.Users.GetUserPermissionsAsync(user.Id);
        var token = tokenService.GenerateToken(user, permissions);

        return new LoginResult(Token: token);
    }
}