namespace UserManagement.API.Features.Authentication.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Matches(@"(\+98|0)?9\d{9}$").WithMessage("فرمت شماره موبایل معتبر نیست.");

        // اگر پسورد ارسال شده باشد، باید معتبر باشد
        When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("رمز عبور نمی‌تواند خالی باشد.")
                .MinimumLength(6).WithMessage("رمز عبور باید حداقل ۶ کاراکتر باشد.");
        });
    }
}