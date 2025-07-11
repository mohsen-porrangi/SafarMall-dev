﻿namespace UserManagement.API.Features.Authentication.Commands.Register
{
    public class LoginCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public LoginCommandValidator()
        {
            //RuleFor(x => x.Email)
            // .NotEmpty().WithMessage("ایمیل الزامی است.")
            // .EmailAddress().WithMessage("فرمت ایمیل معتبر نیست.");

            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("شماره موبایل الزامی است.")
                .Matches(@"(\+98|0)?9\d{9}$").WithMessage("فرمت شماره موبایل معتبر نیست.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("رمز عبور الزامی است.")
                .MinimumLength(6).WithMessage("رمز عبور باید حداقل ۶ کاراکتر باشد.");
        }
    }
}
