namespace SMS.API.Models.Requests;
public class KavenegarSendSmsRequestModel
{
    /// <summary>
    /// شماره دریافت کننده پیامک را مشخص می کند که می توان با کاراکتر ویرگول « , » آنها را از هم جدا کرد
    /// </summary>
    public required string Receptor { get; set; }
    /// <summary>
    /// encode شده
    /// </summary>
    public required string Message { get; set; }
    /// <summary>
    /// در صورت عدم ارسال پارامتر Sender پیامک با ارسال کننده پیش فرض (حساب کاربری کاوه نگار) ارسال خواهد شد
    /// </summary>
    public string? Sender { get; set; } = null;
    /// <summary>
    /// زمان ارسال پیام ( در صورتی که خالی باشد پیام به صورت خودکار در همان لحظه ارسال می‌شود )
    /// date باید Unix باشد
    /// </summary>
    public long? Date { get; set; } = null;
    public string? Type { get; set; } = null;
    /// <summary>
    /// به وسیله مقداردهی به این پارامتر می‌توانید از ارسال پیامک تکراری جلوگیری نمائید
    /// در صورت مقداردهی ،تعداد آن باید برابر تعداد گیرنده باشد و با کاراکتر ویرگول  « , » آنها را از هم جدا کنید
    /// </summary>
    public long? Localid { get; set; } = null;
    /// <summary>
    /// اگر مقداری عددی پارامتر hide برابر 1 باشد شماره گیرنده در فهرست ارسال ها و کنسول وب نمایش داده نمی شود
    /// </summary>
    public byte? Hide { get; set; } = null;

}
