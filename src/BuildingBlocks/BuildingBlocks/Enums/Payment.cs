
namespace BuildingBlocks.Enums
{
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
    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public enum PaymentGatewayType
    {
        /// <summary>
        /// زرین پال
        /// </summary>
        ZarinPal = 1,

        /// <summary>
        /// زیبال
        /// </summary>
        Zibal = 2,

        /// <summary>
        /// محیط تست
        /// </summary>
        Sandbox = 99
    }
}
