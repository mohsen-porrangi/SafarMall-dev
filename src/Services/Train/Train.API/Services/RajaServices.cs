using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using Microsoft.Extensions.Options;
using Simple.Application.model.Enums;
using Train.API.Models.Middle;
using Train.API.Models.OptionalModels;
using Train.API.Models.Requests;
using Train.API.Models.Responses;
using static BuildingBlocks.Contracts.Services.IOrderExternalService;

namespace Train.API.Services;

public class RajaServices(IIntegrationService integrationService, IOptions<TrainWrapper> options , IRedisCacheService redis)
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

            // 🔹 تولید ReserveKey فقط برای مواردی که IsActiveBooking == true
            if (data.ReturnResult != null && data.ReturnResult.Any())
            {
                foreach (var departTrain in data.DepartResult)
                {
                    foreach (var returnTrain in data.ReturnResult)
                    {
                        var tokenRequest = new GenerateReserveKeyRequestDTO
                        {
                            DepartSelectedTrain = departTrain,
                            ReturnSelectedTrain = returnTrain
                        };

                        departTrain.ReserveToken = departTrain.Data.IsActiveBooking
                            ? await GenerateReserveTokenAsync(tokenRequest)
                            : string.Empty;

                        returnTrain.ReserveToken = returnTrain.Data.IsActiveBooking
                            ? await GenerateReserveTokenAsync(tokenRequest)
                            : string.Empty;
                    }
                }
            }
            else
            {
                foreach (var train in data.DepartResult)
                {
                    train.ReserveToken = train.Data.IsActiveBooking
                        ? await GenerateReserveTokenAsync(new GenerateReserveKeyRequestDTO
                        {
                            DepartSelectedTrain = train,
                            ReturnSelectedTrain = null
                        })
                        : string.Empty;
                }
            }

            var searchId = await redis.IncrementAsync("TrainSearch:IdCounter", BusinessPrefixKeyEnum.TrainSearch);
            data.searchId = searchId;
            await redis.SetAsync(searchId.ToString(), data, TimeSpan.FromMinutes(20), BusinessPrefixKeyEnum.TrainSearch);

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

            return res.Data!;
        }

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task<List<PriceDetailTrainResponseDTO>> GetPriceDetailTrainAsync(string reserveToken)
    {
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetPriceDetailTrain";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<PriceDetailTrainResponseDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, reserveToken, "ReserveToken");

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
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetSelectedTrainReserved";
        var res = await integrationService.GetAsync<BaseResponseDTO<ReservedTrainResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, reserveToken, "token");

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
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetOptionalServices";
        var res = await integrationService.GetAsync<BaseResponseDTO<OptionalServiceTrainsResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, reserveToken, "ReserveToken");

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
        string acctionName = $"{trainWrapperSettings.BaseRoute}GetFreeService";
        var res = await integrationService.GetAsync<BaseResponseDTO<TrainFreeServiceResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, reserveToken, "ReserveToken");

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
        string acctionName = $"{trainWrapperSettings.BaseRoute}ReserveCancelation";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<ReserveCancelationResponseDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, reserveToken, "ReserveToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);

            return res.Data!;
        }

        //TODO: Update Statuse Reserved To Canselation

        throw new BadRequestException("خطای ناشناخته");
    }
    public async Task ToTrainPeymentAsync(int orderId)
    {

        //TODO: get confirmation token with orderId from database
        string confirmToken = "";//....

        string acctionName = $"{trainWrapperSettings.BaseRoute}GetConfirmationReserveModel";
        var res = await integrationService.GetAsync<BaseResponseDTO<List<TrainReservedDTO>>>
            (trainWrapperSettings.BaseUrl, acctionName, confirmToken, "confirmToken");

        if (res is not null)
        {
            if (res.IsSuccess is false)
                throw new BadRequestException(res.ErrorMessage!);
        }

        var fullprice = res!.Data!.Sum(a => a.FullPrice);


        //TODO: redirect to zibal with fullprice
    }

    //call this method in ziball call back
    public async Task<ConfirmingResrvedTicketResponseDTO> ConfirmReservedAsync(int orderId)
    {
        //TODO: get confirmation token with orderId from database
        string confirmToken = "";//....

        string acctionName = $"{trainWrapperSettings.BaseRoute}ConfirmReserved";
        var res = await integrationService.GetAsync<BaseResponseDTO<ConfirmingResrvedTicketResponseDTO>>
            (trainWrapperSettings.BaseUrl, acctionName, confirmToken, "confirmToken");

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
