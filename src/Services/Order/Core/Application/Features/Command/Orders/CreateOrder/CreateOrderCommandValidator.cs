using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using FluentValidation;

namespace Order.Application.Features.Command.Orders.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ServiceType)
            .IsInEnum().WithMessage("نوع سرویس معتبر نیست");

        /* RuleFor(x => x.SourceCode)
             .GreaterThan(0).WithMessage("کد مبدا معتبر نیست");

         RuleFor(x => x.DestinationCode)
             .GreaterThan(0).WithMessage("کد مقصد معتبر نیست")
             .NotEqual(x => x.SourceCode).WithMessage("مبدا و مقصد نمی‌توانند یکسان باشند"); TODO پرهام میگه کد رو نیاز نداریم توی order*/

        RuleFor(x => x.SourceName)
            .NotEmpty().WithMessage("نام مبدا الزامی است")
            .MaximumLength(100).WithMessage("نام مبدا نباید بیش از 100 کاراکتر باشد");

        RuleFor(x => x.DestinationName)
            .MaximumLength(100).WithMessage("نام مقصد نباید بیش از 100 کاراکتر باشد");

        RuleFor(x => x.DepartureDate)
            .GreaterThan(DateTime.Today).WithMessage("تاریخ رفت باید در آینده باشد");

        RuleFor(x => x.ReturnDate)
            .GreaterThan(x => x.DepartureDate)
            .WithMessage("تاریخ برگشت باید بعد از تاریخ رفت باشد")
            .When(x => !string.IsNullOrWhiteSpace(x.DestinationName) && x.ReturnDate.HasValue);

        RuleFor(x => x.Passengers)
            .NotEmpty().WithMessage("حداقل یک مسافر الزامی است")
            .Must(x => x.Count <= 10).WithMessage("حداکثر 10 مسافر در هر سفارش مجاز است");

        //  Validation برای اطلاعات پرواز/قطار
        RuleFor(x => x.FlightNumber)
            .NotEmpty().WithMessage("شماره پرواز الزامی است")
            .When(x => x.ServiceType == ServiceType.DomesticFlight ||
                      x.ServiceType == ServiceType.InternationalFlight);

        RuleFor(x => x.TrainNumber)
            .NotEmpty().WithMessage("شماره قطار الزامی است")
            .When(x => x.ServiceType == ServiceType.Train);

        RuleFor(x => x.ProviderId)
            .GreaterThan(0).WithMessage("شرکت ارائه‌دهنده باید انتخاب شود");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("قیمت پایه باید بیشتر از صفر باشد");

        //  Validation ساده‌تر برای مسافران
        RuleForEach(x => x.Passengers).ChildRules(passenger =>
        {
            // حداقل یکی از نام‌ها (فارسی یا انگلیسی) باید پر باشد
            passenger.RuleFor(x => x)
                .Must(p =>
                    !string.IsNullOrWhiteSpace(p.FirstNameEn) && !string.IsNullOrWhiteSpace(p.LastNameEn) ||
                    !string.IsNullOrWhiteSpace(p.FirstNameFa) && !string.IsNullOrWhiteSpace(p.LastNameFa)
                )
                .WithMessage("حداقل یکی از نام‌های فارسی یا انگلیسی باید وارد شود");

            // اگر انگلیسی پر شده، معتبر باشد
            passenger.RuleFor(x => x.FirstNameEn)
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("نام انگلیسی فقط باید شامل حروف انگلیسی باشد")
                .MaximumLength(50).WithMessage("نام انگلیسی نباید بیش از 50 کاراکتر باشد")
                .When(x => !string.IsNullOrWhiteSpace(x.FirstNameEn));

            passenger.RuleFor(x => x.LastNameEn)
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("نام خانوادگی انگلیسی فقط باید شامل حروف انگلیسی باشد")
                .MaximumLength(50).WithMessage("نام خانوادگی انگلیسی نباید بیش از 50 کاراکتر باشد")
                .When(x => !string.IsNullOrWhiteSpace(x.LastNameEn));

            // اگر فارسی پر شده، معتبر باشد
            passenger.RuleFor(x => x.FirstNameFa)
                .Matches(@"^[\u0600-\u06FF\s]+$").WithMessage("نام فارسی فقط باید شامل حروف فارسی باشد")
                .MaximumLength(50).WithMessage("نام فارسی نباید بیش از 50 کاراکتر باشد")
                .When(x => !string.IsNullOrWhiteSpace(x.FirstNameFa));

            passenger.RuleFor(x => x.LastNameFa)
                .Matches(@"^[\u0600-\u06FF\s]+$").WithMessage("نام خانوادگی فارسی فقط باید شامل حروف فارسی باشد")
                .MaximumLength(50).WithMessage("نام خانوادگی فارسی نباید بیش از 50 کاراکتر باشد")
                .When(x => !string.IsNullOrWhiteSpace(x.LastNameFa));

            passenger.RuleFor(x => x.BirthDate)
                .ValidateBirthDate();

            passenger.RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("جنسیت معتبر نیست");

            // کد ملی یا پاسپورت
            passenger.RuleFor(x => x.NationalCode!)
                .ValidationIranianNationalCode()
                .When(x => x.IsIranian);

            passenger.RuleFor(x => x.PassportNo)
                .ValidatePassportNo()
                .When(x => !x.IsIranian);

            passenger.RuleFor(x => x)
                .Must(x => x.IsIranian && !string.IsNullOrWhiteSpace(x.NationalCode) ||
                           !x.IsIranian && !string.IsNullOrWhiteSpace(x.PassportNo))
                .WithMessage("برای اتباع ایرانی کد ملی و برای اتباع خارجی شماره پاسپورت الزامی است");
        });
    }
}