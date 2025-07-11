using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using Carter;
using Microsoft.AspNetCore.Mvc;
using Train.API.Models.Requests;
using Train.API.Services;

namespace Train.API.Endpoints;

public class RajaEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/raja")
            .WithTags("Raja Train Services");

        // دریافت لیست ایستگاه ها
        group.MapGet("/GetStationAsync", async (
            string? filterName,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetStationsAsync(filterName);
            return Results.Ok(result);
        })
        .WithName("GetStationAsync")
        .WithSummary("دریافت لیست ایستگاه های قطار")
        .WithDescription("دریافت لیست تمام ایستگاه های قطار با قابلیت فیلتر بر اساس نام")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // جستجوی قطار
        group.MapPost("/SearchTrain", async (
            [FromBody] SearchTrainRequestDTO request,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var trains = await rajaServices.SearchAsync(request);
            return Results.Ok(trains);
        })
        .WithName("SearchTrain")
        .WithSummary("سرویس سرچ قطار های فعال")
        .WithDescription("جستجوی قطارهای فعال بر اساس مبدا، مقصد و تاریخ")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ایستگاه های میانی
        group.MapGet("/GetIntermediateStationsInfo", async (
            IntermediateStationsInfoRequestDTO request,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.IntermediateStationsInfoAsync(request);
            return Results.Ok(result);
        })
        .WithName("GetIntermediateStationsInfo")
        .WithSummary("دریافت ایستگاه های میانی")
        .WithDescription("دریافت اطلاعات و ایستگاه های مابین مبدا و مقصد")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // تولید توکن رزرو
        group.MapPost("/GenerateReserveToken", async (
            [FromBody] GenerateReserveKeyRequestDTO request,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GenerateReserveTokenAsync(request);
            return Results.Ok(result);
        })
        .WithName("GenerateReserveToken")
        .WithSummary("تولید توکن رزرو")
        .WithDescription("برای صفحه دریافت اطلاعات و ارتباط با سرویس رزرو این توکن ضروری است")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // دریافت قطار انتخاب شده
        group.MapGet("/GetSelectedTrainReserved", async (
            string token,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetSelectedTrainAsync(token);
            return Results.Ok(result);
        })
        .WithName("GetSelectedTrainReserved")
        .WithSummary("دریافت قطار رزرو شده")
        .WithDescription("دریافت قطار های رزرو شده با استفاده از توکن رزرو")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // جزئیات قیمت
        group.MapGet("/GetPriceDetailTrain", async (
            string ReserveToken,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetPriceDetailTrainAsync(ReserveToken);
            return Results.Ok(result);
        })
        .WithName("GetPriceDetailTrain")
        .WithSummary("دریافت جزئیات قیمت")
        .WithDescription("دریافت نرخ های قیمتی به ازای هر قطاری که انتخاب شده")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // خدمات اختیاری پولی
        group.MapGet("/GetOptionalServices", async (
            string ReserveToken,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetOptionalServicesAsync(ReserveToken);
            return Results.Ok(result);
        })
        .WithName("GetOptionalServices")
        .WithSummary("دریافت خدمات اختیاری پولی")
        .WithDescription("دریافت غذا و یا خدمات اختیاری هزینه دار به ازای قطار های انتخاب شده")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // خدمات رایگان
        group.MapGet("/GetFreeService", async (
            string ReserveToken,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetFreeServiceAsync(ReserveToken);
            return Results.Ok(result);
        })
        .WithName("GetFreeService")
        .WithSummary("دریافت خدمات رایگان")
        .WithDescription("دریافت غذا و یا خدمات اختیاری رایگان به ازای قطار های انتخاب شده")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // کپچا
        group.MapGet("/GetCaptcha", async (
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetCaptchaAsync();
            return Results.Ok(result);
        })
        .WithName("GetCaptcha")
        .WithSummary("دریافت کپچا")
        .WithDescription("تولید کپچا برای رزرو قطار")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);

        // رزرو قطار
        group.MapPost("/ReserveTrain", async (
            [FromBody] TrainReserveRequest request,
            [FromServices] ICurrentUserService userService,
            [FromServices] ITrainService trainService,
            CancellationToken ct) =>
        {
            var userId = userService.GetCurrentUserId();
            var result = await trainService.ReserveTrainWithEventAsync(request, userId);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    success = true,
                    message = "رزرو با موفقیت انجام شد",
                    reservationIds = result.CreatedReservationIds
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        })
        .WithName("ReserveTrain")
        .WithSummary("رزرو موقت قطار")
        .WithDescription("انجام رزرو موقت قطار برای کاربر")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // لغو رزرو
        group.MapGet("/ReserveCancelation", async (
            string ReserveToken,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.ReserveCancelationAsync(ReserveToken);
            return Results.Ok(result);
        })
        .WithName("ReserveCancelation")
        .WithSummary("لغو رزرو موقت")
        .WithDescription("لغو رزرو موقت قطار با استفاده از توکن رزرو")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // انتقال به پرداخت
        group.MapGet("/ToPay", async (
            int orderId,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            await rajaServices.ToTrainPeymentAsync(orderId);
            return Results.Ok();
        })
        .WithName("ToPay")
        .WithSummary("انتقال به درگاه پرداخت")
        .WithDescription("انتقال کاربر به درگاه پرداخت برای تکمیل خرید")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // کال بک پرداخت
        group.MapGet("/TrainCallback", async (
            long trackId,
            int success,
            int status,
            int orderId,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            await rajaServices.ConfirmReservedAsync(orderId);
            return Results.Ok();
        })
        .WithName("TrainCallback")
        .WithSummary("کال بک پرداخت")
        .WithDescription("پردازش نتیجه پرداخت از درگاه")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // تایید رزرو
        group.MapGet("/ConfirmReserved", async (
            int orderNumber,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.ConfirmReservedAsync(orderNumber);
            return Results.Ok(result);
        })
        .WithName("ConfirmReserved")
        .WithSummary("تایید رزرو")
        .WithDescription("تایید نهایی رزرو پس از پرداخت موفق")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // حذف بلیط
        group.MapPost("/DeleteTicketWithoutToken", async (
            [FromBody] DeleteTicketRequestDTO request,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.DeleteTicketAsync(request);
            return Results.Ok(result);
        })
        .WithName("DeleteTicketWithoutToken")
        .WithSummary("حذف بلیط")
        .WithDescription("حذف بلیط تایید شده تا بیست دقیقه بدون ضرر")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // جزئیات بلیط
        group.MapGet("/GetTicketDetail", async (
            int ticketNumber,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GetInformationPurchasedTrainAsync(ticketNumber);
            return Results.Ok(result);
        })
        .WithName("GetTicketDetail")
        .WithSummary("جزئیات بلیط")
        .WithDescription("دریافت جزئیات کامل بلیط خریداری شده")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // لینک پرینت بلیط
        group.MapPost("/GetTicketPrintURL", async (
            [FromBody] List<int> ticketNumbers,
            [FromServices] RajaServices rajaServices,
            CancellationToken ct) =>
        {
            var result = await rajaServices.GenerateTicketPrintURLAsync(ticketNumbers);
            return Results.Ok(result);
        })
        .WithName("GetTicketPrintURL")
        .WithSummary("لینک پرینت بلیط")
        .WithDescription("تولید لینک پرینت برای یک یا چند بلیط")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}