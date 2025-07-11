using BuildingBlocks.Exceptions;
using BuildingBlocks.Messaging.Contracts;
using UserManagement.API.Features.Authentication.Commands.Login;

namespace UserManagement.API.Features.Authentication.Commands.VerifyRegistrationOtp;

internal sealed class VerifyRegisterOtpCommandHandler(
    IUnitOfWork uow,
    IOtpService otpService,
   // IWalletService walletService,
    ITemporaryRegistrationService tempRegistrationService,
    ITokenService tokenService,
    ILogger<VerifyRegisterOtpCommandHandler> logger,
    IMessageBus messageBus
) : ICommandHandler<VerifyRegisterOtpCommand, LoginResult>
{
    public async Task<LoginResult> Handle(VerifyRegisterOtpCommand command, CancellationToken cancellationToken)
    {
        Guid UserId;
        User user;
        // دریافت اطلاعات ثبت‌نام موقت
        var tempData = await tempRegistrationService.GetTemporaryRegistrationAsync(command.Mobile);
        if (tempData == null)
        {
            logger.LogWarning("No temporary registration found for mobile: {Mobile}", command.Mobile);
            throw new BadRequestException("درخواست ثبت‌نام یافت نشد",
                "لطفاً ابتدا فرآیند ثبت‌نام را شروع کنید یا مجدداً تلاش کنید");
        }
        // بررسی انقضای زمان (اضافی برای اطمینان)
        if (DateTime.UtcNow - tempData.CreatedAt > TimeSpan.FromMinutes(2))
        {
            logger.LogWarning("Expired registration attempt for mobile: {Mobile}", command.Mobile);
            await tempRegistrationService.RemoveTemporaryRegistrationAsync(command.Mobile);
            throw new BadRequestException("زمان ثبت‌نام به پایان رسیده است",
                "لطفاً فرآیند ثبت‌نام را مجدداً شروع کنید");
        }


        // تأیید OTP
        var isOtpValid = await otpService.ValidateOtpAsync(command.Mobile, command.Otp);
        if (!isOtpValid)
        {
            logger.LogWarning("Invalid OTP provided for mobile: {Mobile}", command.Mobile);
            throw new UnauthorizedDomainException("کد تأیید نامعتبر است",
                "لطفاً کد تأیید ارسال شده را بررسی کنید");
        }
        // شروع تراکنش برای ایجاد کاربر و کیف پول
        using var transaction = await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            // بررسی مجدد عدم تکرار در دیتابیس (برای اطمینان)
            var mobileStillExists = await uow.Users.MobileExistsAsync(command.Mobile, cancellationToken);
            if (mobileStillExists)
            {
                logger.LogError("Mobile became duplicate during registration process: {Mobile}", command.Mobile);
                throw new BadRequestException("شماره موبایل در حین فرآیند ثبت‌نام تکراری شده است",
                    "لطفاً مجدداً تلاش کنید");
            }

            // ایجاد Identity
            var identity = new MasterIdentity
            {
                Id = Guid.NewGuid(),
                Mobile = command.Mobile,
                PasswordHash = tempData.PasswordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true  //  فعال چون OTP تأیید شده
            };

            // ایجاد User
            user = new User
            {
                Id = Guid.NewGuid(),
                IdentityId = identity.Id,
                Name = string.Empty,
                Family = string.Empty,
                NationalCode = string.Empty,
                Gender = null,
                BirthDate = default,
                CreatedAt = DateTime.UtcNow
            };

            // ذخیره در دیتابیس
            await uow.Users.AddIdentityAsync(identity, cancellationToken);
            await uow.Users.AddAsync(user, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            // Commit user creation
            await uow.CommitTransactionAsync(cancellationToken);

            var userActivatedEvent = new UserActivatedEvent(user.Id, command.Mobile);
            logger.LogWarning("Publishing event: Type={EventType}, Assembly={Assembly}",
                userActivatedEvent.GetType().FullName,
                userActivatedEvent.GetType().Assembly.FullName);

            await messageBus.PublishAsync(userActivatedEvent, cancellationToken);

            // ONLY publish event - wallet will be created asynchronously
      //      await messageBus.PublishAsync(new UserActivatedEvent(user.Id, command.Mobile), cancellationToken);

            logger.LogInformation("User registered successfully: {UserId} for mobile: {Mobile}, wallet creation event sent", user.Id, command.Mobile);

            //// ساخت کیف پول بعد از ساخت کامل یوزر
            //var walletCreated = await walletService.CreateWalletAsync(user.Id, cancellationToken);
            //if (!walletCreated)
            //{
            //    logger.LogError("Failed to create wallet for user: {UserId}", user.Id);
            //    throw new InternalServerException("خطا در ایجاد کیف پول", "کاربر ایجاد شد اما کیف پول ساخته نشد");
            //}
            //logger.LogInformation("Wallet created successfully for user: {UserId}", user.Id);

            //await messageBus.PublishAsync(new UserActivatedEvent(user.Id, user.MasterIdentity.Mobile), cancellationToken);




            // commit تراکنش


            // پاک‌سازی اطلاعات موقت
            await tempRegistrationService.RemoveTemporaryRegistrationAsync(command.Mobile);

            logger.LogInformation("Registration completed successfully for mobile: {Mobile}, userId: {UserId}",
                command.Mobile, user.Id);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during user creation process for mobile: {Mobile}", command.Mobile);

            // rollback تراکنش
            await uow.RollbackTransactionAsync(cancellationToken);

            // در صورت خطا، اطلاعات موقت را حفظ می‌کنیم تا کاربر بتواند مجدداً تلاش کند
            if (ex is not BadRequestException)
            {
                throw new InternalServerException("خطا در تکمیل فرآیند ثبت‌نام",
                    "لطفاً مجدداً کد تأیید را وارد کنید یا با پشتیبانی تماس بگیرید");
            }
            throw;
        }

        // Generate token and return
        var permissions = await uow.Users.GetUserPermissionsAsync(user.Id);
        var token = tokenService.GenerateToken(user, permissions);

        return new LoginResult(/*Success: true,*/ Token: token);
    }
}
