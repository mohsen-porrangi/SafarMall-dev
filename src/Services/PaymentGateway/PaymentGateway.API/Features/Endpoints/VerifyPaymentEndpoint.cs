using BuildingBlocks.Enums;
using Carter;
using MediatR;
using PaymentGateway.API.Features.Command.VerifyPayment;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint تایید پرداخت
/// </summary>
public class VerifyPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // POST endpoint برای تایید دستی
        app.MapPost("/api/payments/verify", async (
            VerifyPaymentCommand command,
            IMediator mediator,
            CancellationToken cancellationToken
            ) =>
        {

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                return Results.Ok(new
                {
                    success = true,
                    verified = result.IsVerified,
                    paymentId = result.PaymentId,
                    transactionId = result.TransactionId,
                    trackingCode = result.TrackingCode,
                    amount = result.Amount,
                    status = result.Status.ToString(),
                    verificationDate = result.VerificationDate
                });
            }

            return Results.BadRequest(new
            {
                success = false,
                verified = false,
                paymentId = result.PaymentId,
                status = result.Status.ToString(),
                error = result.ErrorMessage,
                errorCode = result.ErrorCode
            });
        })
            .WithName("VerifyPayment")
            .WithTags("Payment")
            .WithSummary("Verify payment")
            .WithDescription("تأیید پرداخت با درگاه");
        // ZarinPal callback
        app.MapGet("/api/payments/callback/zarinpal", async (
            string Authority,
            string Status,
            IMediator mediator,
            IConfiguration configuration,
            decimal? Amount = null,
            CancellationToken cancellationToken = default
            ) =>
        {
            var command = new VerifyPaymentCommand
            {
                GatewayReference = Authority,
                Status = Status,
                Amount = Amount,
                GatewayType = PaymentGatewayType.ZarinPal
            };

            var result = await mediator.Send(command, cancellationToken);

            // دریافت URL صفحه نتیجه از appsettings
            var resultPageUrl = configuration["UI:DirectPaymentResultUrl"] ?? "/payment-result";

            // ساخت querystring
            var queryParams = new Dictionary<string, string>
            {
                ["success"] = (result.IsSuccessful && result.IsVerified).ToString().ToLower(),
                ["paymentId"] = result.PaymentId ?? "",
                ["status"] = result.Status.ToString(),
                ["gateway"] = "zarinpal"
            };

            if (result.IsSuccessful && result.IsVerified)
            {
                queryParams["transactionId"] = result.TransactionId ?? "";
                queryParams["trackingCode"] = result.TrackingCode ?? "";
                queryParams["amount"] = result.Amount?.ToString() ?? "";
            }
            else
            {
                queryParams["error"] = result.ErrorMessage ?? "پرداخت ناموفق";
                queryParams["errorCode"] = result.ErrorCode ?? "";
            }

            var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            var redirectUrl = $"{resultPageUrl}?{queryString}";

            return Results.Redirect(redirectUrl);
        })
            .WithName("ZarinPalCallback")
            .WithTags("Payment")
            .AllowAnonymous();

        // Zibal callback  
        app.MapGet("/api/payments/callback/zibal", async (
            int success,
            int status,
            long trackId,
            IMediator mediator,
            IConfiguration configuration,
            string? orderId = null,
            CancellationToken cancellationToken = default
            ) =>
        {
            var command = new VerifyPaymentCommand
            {
                GatewayReference = trackId.ToString(),
                Status = success == 1 ? "OK" : "NOK",
                GatewayType = PaymentGatewayType.Zibal
            };

            var result = await mediator.Send(command, cancellationToken);

            var resultPageUrl = configuration["UI:DirectPaymentResultUrl"] ?? "/payment-result";

            var queryParams = new Dictionary<string, string>
            {
                ["success"] = (result.IsSuccessful && result.IsVerified).ToString().ToLower(),
                ["paymentId"] = result.PaymentId ?? "",
                ["status"] = result.Status.ToString(),
                ["gateway"] = "zibal"
            };

            if (result.IsSuccessful && result.IsVerified)
            {
                queryParams["transactionId"] = result.TransactionId ?? "";
                queryParams["trackingCode"] = result.TrackingCode ?? "";
                queryParams["amount"] = result.Amount?.ToString() ?? "";
            }
            else
            {
                queryParams["error"] = result.ErrorMessage ?? "پرداخت ناموفق";
                queryParams["errorCode"] = result.ErrorCode ?? "";
            }

            var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            var redirectUrl = $"{resultPageUrl}?{queryString}";

            return Results.Redirect(redirectUrl);
        })
            .WithName("ZibalCallback")
            .WithTags("Payment")
            .AllowAnonymous();

        // Sandbox callback
        app.MapGet("/api/payments/callback/sandbox", async (
            string Authority,
            string Status,
            IMediator mediator,
            IConfiguration configuration,
            decimal? Amount = null,
            CancellationToken cancellationToken = default
            ) =>
        {
            var command = new VerifyPaymentCommand
            {
                GatewayReference = Authority,
                Status = Status,
                Amount = Amount,
                GatewayType = PaymentGatewayType.Sandbox
            };

            var result = await mediator.Send(command, cancellationToken);

            var resultPageUrl = configuration["UI:DirectPaymentResultUrl"] ?? "/payment-result";

            var queryParams = new Dictionary<string, string>
            {
                ["success"] = (result.IsSuccessful && result.IsVerified).ToString().ToLower(),
                ["paymentId"] = result.PaymentId ?? "",
                ["status"] = result.Status.ToString(),
                ["gateway"] = "sandbox"
            };

            if (result.IsSuccessful && result.IsVerified)
            {
                queryParams["transactionId"] = result.TransactionId ?? "";
                queryParams["trackingCode"] = result.TrackingCode ?? "";
                queryParams["amount"] = result.Amount?.ToString() ?? "";
            }
            else
            {
                queryParams["error"] = result.ErrorMessage ?? "پرداخت ناموفق";
                queryParams["errorCode"] = result.ErrorCode ?? "";
            }

            var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            var redirectUrl = $"{resultPageUrl}?{queryString}";

            return Results.Redirect(redirectUrl);
        })
            .WithName("SandboxCallback")
            .WithTags("Payment")
            .AllowAnonymous();

    }



    private static string GenerateSuccessPage(string paymentId, decimal amount, string? trackingCode)
    {
        return $@"
                <!DOCTYPE html>
                <html dir='rtl' lang='fa'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>پرداخت موفق</title>
                    <style>
                        body {{ font-family: 'Tahoma', sans-serif; text-align: center; margin: 50px; background: #f5f5f5; }}
                        .container {{ background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); max-width: 500px; margin: 0 auto; }}
                        .success {{ color: #4CAF50; }}
                        .info {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='success'>پرداخت موفق</h1>
                        <div class='info'>
                            <p><strong>شناسه پرداخت:</strong> {paymentId}</p>
                            <p><strong>مبلغ:</strong> {amount:N0} ریال</p>
                            {(string.IsNullOrEmpty(trackingCode) ? "" : $"<p><strong>کد پیگیری:</strong> {trackingCode}</p>")}
                        </div>
                        <p>پرداخت شما با موفقیت انجام شد.</p>
                        <button onclick='window.close()'>بستن</button>
                    </div>
                </body>
                </html>";
    }

    private static string GenerateFailurePage(string errorMessage)
    {
        return $@"
                <!DOCTYPE html>
                <html dir='rtl' lang='fa'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>پرداخت ناموفق</title>
                    <style>
                        body {{ font-family: 'Tahoma', sans-serif; text-align: center; margin: 50px; background: #f5f5f5; }}
                        .container {{ background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); max-width: 500px; margin: 0 auto; }}
                        .error {{ color: #f44336; }}
                        .info {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='error'> پرداخت ناموفق</h1>
                        <div class='info'>
                            <p>{errorMessage}</p>
                        </div>
                        <p>لطفاً مجدداً تلاش کنید.</p>
                        <button onclick='window.close()'>بستن</button>
                    </div>
                </body>
                </html>";
    }
}