using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Simple.Application.model.Enums;
using Train.API.Models.Middle;
using Train.API.Models.OptionalModels;
using Train.API.Models.Requests;
using Train.API.Models.Responses;
using static BuildingBlocks.Contracts.Services.IOrderExternalService;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Train.API.Services;

public class RajaServices(IIntegrationService integrationService, IOptions<TrainWrapper> options, IRedisCacheService redis)
{
    private readonly TrainWrapper trainWrapperSettings = options.Value;

    //Impelimentations
    public async Task<List<StationResponseDto>> GetStationsAsync(string? stationNameFilter = null)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}ListStations";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<StationResponseDto>>>(trainWrapperSettings.BaseUrl, acctionName, stationNameFilter, "stationNameFilter");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<TrainSearchResponseDTO> SearchAsync(SearchTrainRequestDTO request)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}SearchTrain";
        BaseResponseDTO<TrainSearchResponseDTO>? res =
            await integrationService.GetAsync<BaseResponseDTO<TrainSearchResponseDTO>>(trainWrapperSettings.BaseUrl, acctionName, request);

        if (res is not null)
        {
            if (!res.IsSuccess)
                throw new BadRequestException(res.ErrorMessage!);

            var data = res.Data!;

            // 🔹 فیلتر کردن براساس isActiveBooking
            if (request.isActiveBooking)
            {
                data.DepartResult = data.DepartResult
                    .Where(x => x.Data.IsActiveBooking)
                    .ToList();

                if (data.ReturnResult != null)
                {
                    data.ReturnResult = data.ReturnResult
                        .Where(x => x.Data.IsActiveBooking)
                        .ToList();
                }
            }

            return data;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<IntermediateStationsInfoResponseDTO>> IntermediateStationsInfoAsync(IntermediateStationsInfoRequestDTO request)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetIntermediateStationsInfo";

        var res = await integrationService.GetAsync<BaseResponseDTO<List<IntermediateStationsInfoResponseDTO>>>(trainWrapperSettings.BaseUrl, acctionName, request);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<string> GenerateReserveTokenAsync(GenerateReserveKeyRequestDTO request)
    {

        string acctionName = $"{trainWrapperSettings.BaseRoute}GenerateReserveToken";
        var res = await integrationService.PostAsync<BaseResponseDTO<string>>(trainWrapperSettings.BaseUrl, acctionName, request, ContentTypeEnums.Json);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            var reserveToken = Guid.NewGuid().ToString();

            await redis.SetAsync(reserveToken, res!.Data, TimeSpan.FromMinutes(60), BusinessPrefixKeyEnum.TrainReservation);

            return reserveToken;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<PriceDetailTrainResponseDTO>> GetPriceDetailTrainAsync(string reserveToken)
    {
        var token = await redis.GetAsync<string>(reserveToken, BusinessPrefixKeyEnum.TrainReservation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        string acctionName = $"{trainWrapperSettings.BaseRoute}GetPriceDetailTrain";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<PriceDetailTrainResponseDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "ReserveToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<ReservedTrainResponseDTO> GetSelectedTrainAsync(string reserveToken)
    {

        var token = await redis.GetAsync<string>(reserveToken, BusinessPrefixKeyEnum.TrainReservation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        string acctionName = $"{trainWrapperSettings.BaseRoute}GetSelectedTrainReserved";
        var res = await integrationService.GetAsync<BaseResponseDTO<ReservedTrainResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "token");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<OptionalServiceTrainsResponseDTO> GetOptionalServicesAsync(string reserveToken)
    {
        var token = await redis.GetAsync<string>(reserveToken, BusinessPrefixKeyEnum.TrainReservation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");


        string acctionName = $"{trainWrapperSettings.BaseRoute}GetOptionalServices";
        var res = await integrationService.GetAsync<BaseResponseDTO<OptionalServiceTrainsResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "ReserveToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<TrainFreeServiceResponseDTO> GetFreeServiceAsync(string reserveToken)
    {
        var token = await redis.GetAsync<string>(reserveToken, BusinessPrefixKeyEnum.TrainReservation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        string acctionName = $"{trainWrapperSettings.BaseRoute}GetFreeService";
        var res = await integrationService.GetAsync<BaseResponseDTO<TrainFreeServiceResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "ReserveToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<CaptchaResponseDTO> GetCaptchaAsync()
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetCaptcha";
        var res = await integrationService.GetAsync<BaseResponseDTO<CaptchaResponseDTO>>(trainWrapperSettings.BaseUrl, acctionName, null);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<ReserveResponseDTO> ReserveTrainAsync(TrainReserveRequestDto request)
    {
        var token = await redis.GetAsync<string>(request.ReserveToken, BusinessPrefixKeyEnum.TrainReservation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        //جایگزین کردن توکن سیستمی با توکن اصلی
        request.ReserveToken = token;

        //ارسال درخواست به ترین رپر
        string acctionName = $"{trainWrapperSettings.BaseRoute}ReserveTrain";
        var res = await integrationService.PostAsync<BaseResponseDTO<ReserveResponseDTO>>(trainWrapperSettings.BaseUrl, acctionName, request, ContentTypeEnums.Json);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        //TODO: add order in database with pendeing status and insert confirmation Token in data base and return orderID

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<ReserveCancelationResponseDTO>> ReserveCancelationAsync(string reserveToken)
    {
        var token = await redis.GetAsync<string>(reserveToken, BusinessPrefixKeyEnum.TrainSearch);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        string acctionName = $"{trainWrapperSettings.BaseRoute}ReserveCancelation";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<ReserveCancelationResponseDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "ReserveToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        //TODO: Update Statuse Reserved To Canselation

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task ToTrainPeymentAsync(string reservationId)
    {
        //TODO: get confirmation token with orderId from database
        var confirmModel = await redis.GetAsync<TrainReservationData>(reservationId, BusinessPrefixKeyEnum.TrainReserveConfirmation);
        if (confirmModel is null)
            throw new BadRequestException("شناسه ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        //TODO: redirect to zibal with fullprice
    }

    //call this method in ziball call back
    public async Task<ConfirmingResrvedTicketResponseDTO> ConfirmReservedAsync(string reservationToken)
    {

        var token = await redis.GetAsync<string>(reservationToken, BusinessPrefixKeyEnum.TrainReserveConfirmation);
        if (token is null)
            throw new BadRequestException("توکن ارسالی اشتباه است یا زمان رزرو به پایان رسیده لطفا دوباره سعی کنید");

        string acctionName = $"{trainWrapperSettings.BaseRoute}ConfirmReserved";
        var res = await integrationService.GetAsync<BaseResponseDTO<ConfirmingResrvedTicketResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, token, "confirmToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        //TODO: UpdateOrderStatus

        //TODO: Call TicketPrint service and send URL print with sms 
        var url = GenerateTicketPrintURLAsync(res!.Data!.TicketNumber!);
        //send url withSMS(get phone number from database)

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<int>> DeleteTicketAsync(DeleteTicketRequestDTO request)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}DeleteTicketWithoutToken";
        var res = await integrationService.PostAsync<BaseResponseDTO<List<int>>>(trainWrapperSettings.BaseUrl, acctionName, request, ContentTypeEnums.Json);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<OrderDetailTrainInformationResponseDTO>> GetInformationPurchasedTrainAsync(int trainNumber)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetTicketDetai";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<OrderDetailTrainInformationResponseDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, trainNumber, "ticketNumber");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<string> GenerateTicketPrintURLAsync(List<int> ticketNumbers)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetTicketPrintURL";
        var res = await integrationService.PostAsync<BaseResponseDTO<string>>(trainWrapperSettings.BaseUrl, acctionName, ticketNumbers, ContentTypeEnums.Json);

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
}
