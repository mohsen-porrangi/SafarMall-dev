using BuildingBlocks.Contracts;
using Microsoft.Extensions.Options;
using Simple.Application.model.Enums;
using SMS.API.Models.Enums;
using SMS.API.Models.OptionModels;
using SMS.API.Models.Requests;
using BuildingBlocks.Extensions;
using SMS.API.Models.Responses;
using BuildingBlocks.Utils.SafeLog;
using BuildingBlocks.Exceptions;
using Simple.Infrastructure.SharedService.Caching;

namespace SMS.API.Services;

public class SmsService
{//params
    private readonly IIntegrationService _integrationService;
    private readonly KavenegarSmsOptions _smsPanelSettings;

    //TODO: private readonly IRedisCacheService _redisCacheService;
    private readonly MemoryCacheService _cacheService;

    //CTOR
    public SmsService(IIntegrationService integrationService
                        , IOptions<KavenegarSmsOptions> smsServices,
                          MemoryCacheService cacheService)
    {
        _integrationService = integrationService;
        _smsPanelSettings = smsServices.Value;
        _cacheService = cacheService;
        //_redisCacheService = redisCacheService;
    }

    //Impelimentations

    public async Task<bool> SendCustomSms(List<string> receptor,
        string message,
        string? Sender = null,
        DateTime? Date = null,
        KavenegarSmsTypeEnums? type = null,
        long? Localid = null,
        byte? Hide = null)
    {

        var receptorsStringFormat = JoinStrings(receptor);

        var requestModel = new KavenegarSendSmsRequestModel()
        {
            Receptor = receptorsStringFormat,
            Message = message,
            Date = Date?.DateTimeToUnix(),
            Hide = Hide,
            Localid = Localid,
            Sender = Sender,
            Type = type.HasValue ? ((int)type).ToString() : null
        };

        var content = requestModel.ToKeyValuePairs();
        try
        {
            var result = await _integrationService.PostAsync<KavenegarResponseModel<KavenegarSendSmsResponseModel>>
           (_smsPanelSettings.SmsWebSericeBasicURL,
           string.Concat(_smsPanelSettings.APIKey, _smsPanelSettings.SendSmsServiceRoute),
           content,
           ContentTypeEnums.Json
           );

            return true;
        }
        catch (Exception ex)
        {
            SafeLog.Error(ex.Message, ex.StackTrace);
            return false;
        }
    }


    public async Task SendOTPAsync(string phoneNumber)
    {
        if (!_cacheService.CanAttemptVerification(phoneNumber + "SendOTPStep", 1, _smsPanelSettings.VerificationCodeTTL))
            throw new BadRequestException("کد تایید برای شما ارسال شده لطفا تا پایان زمان مشخص صبوری فرمایید.");

        Random generator = new Random();
        string verificationCode = generator.Next(0, 1000000).ToString("D6");

        var request = new KavenegarSmsVerifyRequestModel()
        {
            Receptor = phoneNumber,
            Template = _smsPanelSettings.WebServiceTemplate,
            Token = verificationCode
        };

        var content = request.ToKeyValuePairs();

        var result = await _integrationService.PostAsync<KavenegarVerifyDetailResponse>
            (_smsPanelSettings.SmsWebSericeBasicURL,
            string.Concat(_smsPanelSettings.APIKey, _smsPanelSettings.VerifyServiceRoute),
            content,
            ContentTypeEnums.FormUrlencodedFormat, null, null, default
            );

        _cacheService.SetValue(verificationCode, true, TimeSpan.FromSeconds(_smsPanelSettings.VerificationCodeTTL));
    }

    public void CheckVarifactionCode(string code, string phoneNumber)
    {
        if (!_cacheService.CanAttemptVerification(phoneNumber + "CheckVerifyStep", 3, _smsPanelSettings.VerificationCodeTTL))
            throw new BadRequestException("شما برای ثبت کد تایید بیش از حد تلاش کرده اید لطفاً بعد از مدتی دوباره امتحان کنید.");

        var result = _cacheService.GetValue(code);

        if (result is not true)
            throw new BadRequestException(".خطا !کد منقضی شده یا نادرست است لطفا مجددا تلاش فرمایید");

        _cacheService.RemoveValue(code);
    }
    //private method
    private string JoinStrings(List<string> items)
    {
        if (items == null || items.Count == 0)
            return string.Empty;

        if (items.Count == 1)
            return items[0];

        return string.Join(",", items);
    }


    #region TODO
    //RedisCache: this method be should replace with microsoft cache

    //public async Task<bool> SendOTPAsync(string phoneNumber)
    //{
    //    if (!await _redisCacheService.ApplyEffortLimit(phoneNumber, BusinessPrefixKeyEnum.OverlimitSendOTP, 1, _smsPanelSettings.VerificationCodeTTL))
    //        throw new BadRequestException("کد تایید برای شما ارسال شده لطفا تا پایان زمان مشخص صبوری فرمایید.");

    //    Random generator = new Random();
    //    string verificationCode = generator.Next(0, 1000000).ToString("D6");

    //    var request = new KavenegarSmsVerifyRequestModel()
    //    {
    //        Receptor = phoneNumber,
    //        Template = _smsPanelSettings.WebServiceTemplate,
    //        Token = verificationCode
    //    };

    //    var content = request.ToKeyValuePairs();
    //    try
    //    {
    //        var result = await _integrationService.PostAsync<KavenegarResponseModel<KavenegarVerifyDetailResponse>>
    //            (_smsPanelSettings.SmsWebSericeBasicURL,
    //            string.Concat(_smsPanelSettings.APIKey, _smsPanelSettings.VerifyServiceRoute),
    //            content,
    //            ContentTypeEnums.FormUrlencodedFormat
    //            );

    //        await _redisCacheService.SetAsync(phoneNumber, verificationCode, TimeSpan.FromSeconds(_smsPanelSettings.VerificationCodeTTL), BusinessPrefixKeyEnum.OTP);

    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        SafeLog.Error(ex.Message, ex.StackTrace);
    //        return false;
    //    }
    //}

    //public async Task<bool> CheckVarifactionCode(string sendingCode, string phoneNumber)
    //{
    //    if (!await _redisCacheService.ApplyEffortLimit(phoneNumber, BusinessPrefixKeyEnum.OverlimitCheckOTP, 3, _smsPanelSettings.VerificationCodeTTL))
    //        throw new BadRequestException("شما برای ثبت کد تایید بیش از حد تلاش کرده اید لطفاً بعد از مدتی دوباره امتحان کنید.");

    //    var verifyCode = await _redisCacheService.GetAsync<string>(phoneNumber, BusinessPrefixKeyEnum.OTP);

    //    if (verifyCode is null)
    //        throw new BadRequestException(".خطا !کد منقضی شده است لطفا مجددا تلاش فرمایید");
    //    else if (verifyCode != sendingCode)
    //        throw new BadRequestException(".خطا !کد ارسال شده نادرست است لطفا مجددا تلاش فرمایید");


    //    await _redisCacheService.RemoveAsync(phoneNumber, BusinessPrefixKeyEnum.OTP);

    //    return true;
    //}
    #endregion
}
