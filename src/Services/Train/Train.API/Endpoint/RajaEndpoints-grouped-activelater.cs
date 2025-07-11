//using BuildingBlocks.Contracts;
//using BuildingBlocks.Contracts.Services;
//using Carter;
//using Train.API.Models.Requests;
//using Train.API.Services;

//namespace Train.API.Endpoints;

//public class RajaEndpoints : ICarterModule
//{
//    public void AddRoutes(IEndpointRouteBuilder app)
//    {
//        var group = app.MapGroup("/api/raja")
//            .WithTags("Raja Train Services");

//        // دریافت لیست ایستگاه ها
//        group.MapGet("/stations", async (
//            string? filterName,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetStationsAsync(filterName);
//            return Results.Ok(result);
//        })
//        .WithName("GetStations")
//        .WithSummary("دریافت لیست ایستگاه های قطار")
//        .WithDescription("دریافت لیست تمام ایستگاه های قطار با قابلیت فیلتر بر اساس نام")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // جستجوی قطار
//        group.MapPost("/search", async (
//            SearchTrainRequestDTO request,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var trains = await rajaServices.SearchAsync(request);
//            return Results.Ok(trains);
//        })
//        .WithName("SearchTrains")
//        .WithSummary("سرویس سرچ قطار های فعال")
//        .WithDescription("جستجوی قطارهای فعال بر اساس مبدا، مقصد و تاریخ")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // ایستگاه های میانی
//        group.MapGet("/intermediate-stations", async (
//            IntermediateStationsInfoRequestDTO request,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.IntermediateStationsInfoAsync(request);
//            return Results.Ok(result);
//        })
//        .WithName("GetIntermediateStations")
//        .WithSummary("دریافت ایستگاه های میانی")
//        .WithDescription("دریافت اطلاعات و ایستگاه های مابین مبدا و مقصد")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // تولید توکن رزرو
//        group.MapPost("/reserve-token", async (
//            GenerateReserveKeyRequestDTO request,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GenerateReserveTokenAsync(request);
//            return Results.Ok(result);
//        })
//        .WithName("GenerateReserveToken")
//        .WithSummary("تولید توکن رزرو")
//        .WithDescription("برای صفحه دریافت اطلاعات و ارتباط با سرویس رزرو این توکن ضروری است")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // دریافت قطار انتخاب شده
//        group.MapGet("/selected-train", async (
//            string token,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetSelectedTrainAsync(token);
//            return Results.Ok(result);
//        })
//        .WithName("GetSelectedTrain")
//        .WithSummary("دریافت قطار رزرو شده")
//        .WithDescription("دریافت قطار های رزرو شده با استفاده از توکن رزرو")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // جزئیات قیمت
//        group.MapGet("/price-details", async (
//            string reserveToken,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetPriceDetailTrainAsync(reserveToken);
//            return Results.Ok(result);
//        })
//        .WithName("GetPriceDetails")
//        .WithSummary("دریافت جزئیات قیمت")
//        .WithDescription("دریافت نرخ های قیمتی به ازای هر قطاری که انتخاب شده")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // خدمات اختیاری پولی
//        group.MapGet("/optional-services", async (
//            string reserveToken,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetOptionalServicesAsync(reserveToken);
//            return Results.Ok(result);
//        })
//        .WithName("GetOptionalServices")
//        .WithSummary("دریافت خدمات اختیاری پولی")
//        .WithDescription("دریافت غذا و یا خدمات اختیاری هزینه دار به ازای قطار های انتخاب شده")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // خدمات رایگان
//        group.MapGet("/free-services", async (
//            string reserveToken,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetFreeServiceAsync(reserveToken);
//            return Results.Ok(result);
//        })
//        .WithName("GetFreeServices")
//        .WithSummary("دریافت خدمات رایگان")
//        .WithDescription("دریافت غذا و یا خدمات اختیاری رایگان به ازای قطار های انتخاب شده")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // کپچا
//        group.MapGet("/captcha", async (
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetCaptchaAsync();
//            return Results.Ok(result);
//        })
//        .WithName("GetCaptcha")
//        .WithSummary("دریافت کپچا")
//        .WithDescription("تولید کپچا برای رزرو قطار")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // رزرو قطار
//        group.MapPost("/reserve", async (
//            TrainReserveRequest request,
//            ICurrentUserService userService,
//            ITrainService trainService,
//            CancellationToken ct) =>
//        {
//            var userId = userService.GetCurrentUserId();
//            var result = await trainService.ReserveTrainWithEventAsync(request, userId);

//            if (result.IsSuccess)
//            {
//                return Results.Ok(new
//                {
//                    success = true,
//                    message = "رزرو با موفقیت انجام شد",
//                    reservationIds = result.CreatedReservationIds
//                });
//            }

//            return Results.BadRequest(new
//            {
//                success = false,
//                message = result.ErrorMessage
//            });
//        })
//        .WithName("ReserveTrain")
//        .WithSummary("رزرو موقت قطار")
//        .WithDescription("انجام رزرو موقت قطار برای کاربر")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status401Unauthorized)
//        .Produces(StatusCodes.Status409Conflict)
//        .Produces(StatusCodes.Status500InternalServerError)
//        .RequireAuthorization();

//        // لغو رزرو
//        group.MapDelete("/reserve", async (
//            string reserveToken,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.ReserveCancelationAsync(reserveToken);
//            return Results.Ok(result);
//        })
//        .WithName("CancelReservation")
//        .WithSummary("لغو رزرو موقت")
//        .WithDescription("لغو رزرو موقت قطار با استفاده از توکن رزرو")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status409Conflict)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // انتقال به پرداخت
//        group.MapPost("/payment", async (
//            int orderId,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            await rajaServices.ToTrainPeymentAsync(orderId);
//            return Results.Ok();
//        })
//        .WithName("ProcessPayment")
//        .WithSummary("انتقال به درگاه پرداخت")
//        .WithDescription("انتقال کاربر به درگاه پرداخت برای تکمیل خرید")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // کال بک پرداخت
//        group.MapGet("/payment/callback", async (
//            long trackId,
//            int success,
//            int status,
//            int orderId,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            await rajaServices.ConfirmReservedAsync(orderId);
//            return Results.Ok();
//        })
//        .WithName("PaymentCallback")
//        .WithSummary("کال بک پرداخت")
//        .WithDescription("پردازش نتیجه پرداخت از درگاه")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // تایید رزرو
//        group.MapPost("/confirm", async (
//            int orderNumber,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.ConfirmReservedAsync(orderNumber);
//            return Results.Ok(result);
//        })
//        .WithName("ConfirmReservation")
//        .WithSummary("تایید رزرو")
//        .WithDescription("تایید نهایی رزرو پس از پرداخت موفق")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status409Conflict)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // حذف بلیط
//        group.MapDelete("/ticket", async (
//            DeleteTicketRequestDTO request,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.DeleteTicketAsync(request);
//            return Results.Ok(result);
//        })
//        .WithName("DeleteTicket")
//        .WithSummary("حذف بلیط")
//        .WithDescription("حذف بلیط تایید شده تا بیست دقیقه بدون ضرر")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status409Conflict)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // جزئیات بلیط
//        group.MapGet("/ticket/{ticketNumber:int}", async (
//            int ticketNumber,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GetInformationPurchasedTrainAsync(ticketNumber);
//            return Results.Ok(result);
//        })
//        .WithName("GetTicketDetails")
//        .WithSummary("جزئیات بلیط")
//        .WithDescription("دریافت جزئیات کامل بلیط خریداری شده")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);

//        // لینک پرینت بلیط
//        group.MapPost("/ticket/print-url", async (
//            List<int> ticketNumbers,
//            RajaServices rajaServices,
//            CancellationToken ct) =>
//        {
//            var result = await rajaServices.GenerateTicketPrintURLAsync(ticketNumbers);
//            return Results.Ok(result);
//        })
//        .WithName("GetTicketPrintUrl")
//        .WithSummary("لینک پرینت بلیط")
//        .WithDescription("تولید لینک پرینت برای یک یا چند بلیط")
//        .Produces(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .Produces(StatusCodes.Status500InternalServerError);
//    }
//}