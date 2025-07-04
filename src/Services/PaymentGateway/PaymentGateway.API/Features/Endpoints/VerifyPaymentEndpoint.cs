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
        app.MapPost("/api/payments/verify", VerifyPaymentAsync)
            .WithName("VerifyPayment")
            .WithTags("Payment")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Verify payment";
                operation.Description = "Verify payment with gateway";
                return operation;
            });

        // GET endpoint برای callback درگاه‌ها
        app.MapGet("/api/payments/callback", PaymentCallbackAsync)
            .WithName("PaymentCallback")
            .WithTags("Payment")
            .AllowAnonymous()
            .WithOpenApi(operation =>
            {
                operation.Summary = "Payment gateway callback";
                operation.Description = "Handle payment gateway callback";
                return operation;
            });
    }

    /// <summary>
    /// مدل درخواست تایید پرداخت
    /// </summary>
    public record VerifyPaymentRequest(
        string GatewayReference,
        string Status = "OK",
        decimal? Amount = null,
        int GatewayType = 1
    );

    private static async Task<IResult> VerifyPaymentAsync(
        VerifyPaymentRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new VerifyPaymentCommand
        {
            GatewayReference = request.GatewayReference,
            Status = request.Status,
            Amount = request.Amount,
            GatewayType = (PaymentGatewayType)request.GatewayType
        };

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
    }

    /// <summary>
    /// Callback درگاه‌های پرداخت
    /// </summary>
    private static async Task<IResult> PaymentCallbackAsync(
           string Authority,
           string Status,
           IMediator mediator,
           decimal? Amount = null,
           int gateway = 1,
           CancellationToken cancellationToken = default)
    {
        var command = new VerifyPaymentCommand
        {
            GatewayReference = Authority,
            Status = Status,
            Amount = Amount,
            GatewayType = (PaymentGatewayType)gateway
        };

        var result = await mediator.Send(command, cancellationToken);

        // برای callback، پاسخ HTML برمی‌گردانیم
        var htmlContent = result.IsSuccessful && result.IsVerified
            ? GenerateSuccessPage(result.PaymentId!, result.Amount!.Value, result.TrackingCode)
            : GenerateFailurePage(result.ErrorMessage ?? "پرداخت ناموفق بود");

        return Results.Content(htmlContent, "text/html");
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