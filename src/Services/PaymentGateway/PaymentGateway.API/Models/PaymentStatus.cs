namespace PaymentGateway.API.Models;

/// <summary>
/// وضعیت پرداخت
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// در انتظار پرداخت
    /// </summary>
    Pending = 1,

    /// <summary>
    /// پرداخت شده
    /// </summary>
    Paid = 2,

    /// <summary>
    /// ناموفق
    /// </summary>
    Failed = 3,

    /// <summary>
    /// لغو شده
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// منقضی شده
    /// </summary>
    Expired = 5,

    /// <summary>
    /// در حال پردازش
    /// </summary>
    Processing = 6
}