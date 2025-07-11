using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Enums;
using Microsoft.AspNetCore.Mvc;
using Train.API.Models.Requests;
using Train.API.Services;

namespace Train.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RajaController(
        RajaServices rajaServices,
        ICurrentUserService userService,
        ITrainService trainService

        ) : ControllerBase
    {
        [HttpGet("GetStationAsync")]
        public async Task<IActionResult> GetStationAsync([FromQuery] string? filterName = null)
        {
            var res = await rajaServices.GetStationsAsync(filterName);
            return Ok(res);
        }

        /// <summary>
        /// سرویس سرچ قطار های فعال
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("SearchTrain")]
        //[EnableRateLimiting("fixed")]
        public async Task<IActionResult> SearchAsync(SearchTrainRequestDTO request)
        {
            var trains = await rajaServices.SearchAsync(request);
            return Ok(trains);

        }

        /// <summary>
        /// سرویس دریافت اطلاعات و ایستگاه های مابین مبدا و مقصد
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("GetIntermediateStationsInfo")]
        //[EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetIntermediateStationsInfoAsync([FromQuery] IntermediateStationsInfoRequestDTO request)
        {
            var result = await rajaServices.IntermediateStationsInfoAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// سرویس دریافت توکن رزرو
        ///(برای صفحه ی دریافت اطلاعات و ارتباط بت سرویس رزرو این توکن ضروری است)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("GenerateReserveToken")]
        public async Task<IActionResult> GenerateReserveTokenAsync([FromBody] GenerateReserveKeyRequestDTO request)
        {
            var result = await rajaServices.GenerateReserveTokenAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// دریافت قطار های رزرو شده با استفاده از توکن رزرو
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("GetSelectedTrainReserved")]
        public async Task<IActionResult> GetSelectedTrainAsync([FromQuery] string token)
        {
            var result = await rajaServices.GetSelectedTrainAsync(token);
            return Ok(result);
        }

        /// <summary>
        /// سرویس دریافت نرخ های قیمتی به ازای هر قطاری که دانتخاب شده
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        [HttpGet("GetPriceDetailTrain")]
        public async Task<IActionResult> GetPriceDetailTrainAsync([FromQuery] string ReserveToken)
        {
            var result = await rajaServices.GetPriceDetailTrainAsync(ReserveToken);
            return Ok(result);
        }

        /// <summary>
        /// سرویس غذا و یا خدمات اختیاری هزینه دار به ازای قطار های انتخاب شده
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        [HttpGet("GetOptionalServices")]
        public async Task<IActionResult> GetOptionalServicesAsync([FromQuery] string ReserveToken)
        {
            var result = await rajaServices.GetOptionalServicesAsync(ReserveToken);
            return Ok(result);
        }

        /// <summary>
        /// سرویس غذا و یا خدمات اختیاری رایگان به ازای قطار های انتخاب شده
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        [HttpGet("GetFreeService")]
        public async Task<IActionResult> GetFreeServiceAsync([FromQuery] string ReserveToken)
        {
            var result = await rajaServices.GetFreeServiceAsync(ReserveToken);
            return Ok(result);
        }

        /// <summary>
        /// سرویس کپچا برای رزرو قطار
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetCaptcha")]
        public async Task<IActionResult> GetCaptchaSync()
        {
            var result = await rajaServices.GetCaptchaAsync();
            return Ok(result);
        }

        /// <summary>
        /// سرویس رزرو موقت قطار
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("ReserveTrainForPassenger")]
        public async Task<IActionResult> ReserveTrainAsync([FromBody] TrainPassengerReserveRequest request)
        {

            var userId = userService.GetCurrentUserId();

            var result = await trainService.ReserveTrainForPassengerWithOrderAsync(request, userId);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = "رزرو با موفقیت انجام شد",
                    reservationIds = result.ReservationId,
                    OrderId = result.OrderId,
                    OrderNumber = result.OrderNumber
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        [HttpPost("ReserveTrainForCar")]
        public async Task<IActionResult> ReserveTrainAsync([FromBody] TrainCarReserveRequest request)
        {

            var userId = userService.GetCurrentUserId();

            var result = await trainService.ReserveTrainForCarWithOrderAsync(request, userId);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = "رزرو با موفقیت انجام شد",
                    reservationIds = result.ReservationId,
                    OrderId = result.OrderId,
                    OrderNumber = result.OrderNumber

                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });
        }

        /// <summary>
        /// سرویس لغو رزرو موقت قطار
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("ReserveCancelation")]
        public async Task<IActionResult> ReserveCancelationAsync([FromQuery] string ReserveToken)
        {
            var result = await rajaServices.ReserveCancelationAsync(ReserveToken);
            return Ok(result);
        }
        /// <summary>
        /// سرویس پرداخت و انتقال به درگاه
        /// </summary>
        /// <param name="confirmToken"></param>
        /// <returns></returns>
        [HttpGet("ToPay")]
        public async Task<IActionResult> ToPayAsync([FromQuery] string ReservationId)
        {
            await rajaServices.ToTrainPeymentAsync(ReservationId);
            return Ok();
        }

        [HttpGet("TrainCallback")]
        public async Task<IActionResult> TrainCallBackAsync([FromQuery] long trackId, [FromQuery] int success, [FromQuery] int status, [FromQuery] int orderId)
        {
            //await rajaServices.ConfirmReservedAsync(orderId);
            return Ok();
        }
        /// <summary>
        /// سرویس تایید رزرو
        /// </summary>
        /// <param name="confirmToken"></param>
        /// <returns></returns>
        [HttpGet("ConfirmReserved")]
        public async Task<IActionResult> ConfirmReservedAsync([FromQuery] string ReservationId)
        {
            var result = await rajaServices.ConfirmReservedAsync(ReservationId);
            return Ok(result);
        }
        /// <summary>
        /// سرویس حذف بلیط تایید شده که تا بیست دقیقه بدون ضرر امکان پذیر است
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("DeleteTicketWithoutToken")]
        public async Task<IActionResult> DeleteTicketAsync([FromBody] DeleteTicketRequestDTO request)
        {
            var result = await rajaServices.DeleteTicketAsync(request);
            return Ok(result);
        }
        /// <summary>
        /// سرویس جزییات بلیط خریداری شده
        /// </summary>
        /// <param name="ticketNumber"></param>
        /// <returns></returns>
        [HttpGet("GetTicketDetai")]
        public async Task<IActionResult> GetTicketDetaiAsync([FromQuery] int ticketNumber)
        {
            var result = await rajaServices.GetInformationPurchasedTrainAsync(ticketNumber);
            return Ok(result);

        }
        /// <summary>
        /// سرویس ایجاد بستری پرینت بلیط برای مسافر
        /// </summary>
        /// <param name="ticketNumbers"></param>
        /// <returns></returns>
        [HttpPost("GetTicketPrintURL")]
        public async Task<IActionResult> GetTicketPrintURLAsync([FromBody] List<int> ticketNumbers)
        {
            var result = await rajaServices.GenerateTicketPrintURLAsync(ticketNumbers);
            return Ok(result);

        }
    }
}
