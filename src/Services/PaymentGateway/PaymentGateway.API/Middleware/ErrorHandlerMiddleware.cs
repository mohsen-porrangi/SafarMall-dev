using BuildingBlocks.Exceptions;
using FluentValidation;
using PaymentGateway.API.Common;
using System.Net;
using System.Text.Json;

namespace PaymentGateway.API.Middleware;

/// <summary>
/// Middleware مدیریت خطاهای کلی
/// </summary>
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(validationEx),
            BadRequestException badRequestEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Bad Request",
                badRequestEx.Message,
                badRequestEx.Details),
            NotFoundException notFoundEx => CreateErrorResponse(
                HttpStatusCode.NotFound,
                "Not Found",
                notFoundEx.Message),
            UnauthorizedDomainException => CreateErrorResponse(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "دسترسی غیرمجاز"),
            ForbiddenDomainException => CreateErrorResponse(
                HttpStatusCode.Forbidden,
                "Forbidden",
                "دسترسی ممنوع"),
            ConflictDomainException conflictEx => CreateErrorResponse(
                HttpStatusCode.Conflict,
                "Conflict",
                conflictEx.Message,
                conflictEx.Details),
            ServiceCommunicationException => CreateErrorResponse(
                HttpStatusCode.ServiceUnavailable,
                "Service Unavailable",
                "خطا در ارتباط با سرویس خارجی"),
            TimeoutException => CreateErrorResponse(
                HttpStatusCode.RequestTimeout,
                "Timeout",
                "درخواست به دلیل انقضای زمان متوقف شد"),
            ArgumentException argumentEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Invalid Argument",
                argumentEx.Message),
            InvalidOperationException invalidOpEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidOpEx.Message),
            HttpRequestException httpEx => CreateErrorResponse(
                HttpStatusCode.BadGateway,
                "Gateway Error",
                DomainErrors.GetMessage(DomainErrors.Gateway.CommunicationError)),
            TaskCanceledException => CreateErrorResponse(
                HttpStatusCode.RequestTimeout,
                "Timeout",
                DomainErrors.GetMessage(DomainErrors.Gateway.TimeoutError)),
            _ => CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "خطای داخلی سرور",
                context.TraceIdentifier)
        };

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ErrorResponse CreateErrorResponse(
        HttpStatusCode statusCode,
        string title,
        string detail,
        string? additionalInfo = null)
    {
        return new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Title = title,
            Detail = detail,
            AdditionalInfo = additionalInfo,
            Timestamp = DateTime.UtcNow
        };
    }

    private static ValidationErrorResponse CreateValidationErrorResponse(ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationErrorResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Title = "Validation Error",
            Detail = "یک یا چند خطای اعتبارسنجی رخ داده است",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// مدل پاسخ خطا
/// </summary>
public record ErrorResponse
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string? AdditionalInfo { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// مدل پاسخ خطای اعتبارسنجی
/// </summary>
public record ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, string[]> Errors { get; init; } = new();
}