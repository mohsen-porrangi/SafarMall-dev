namespace UserManagement.API.Features.Authentication.Commands.VerifyLoginOtp;

public class VerifyLoginOtpCommandValidator : AbstractValidator<VerifyLoginOtpCommand>
{
    public VerifyLoginOtpCommandValidator()
    {
        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Matches(@"(\+98|0)?9\d{9}$").WithMessage("فرمت شماره موبایل معتبر نیست.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Matches(@"^\d{6}$").WithMessage("کد تأیید باید ۶ رقم باشد.");
    }
}