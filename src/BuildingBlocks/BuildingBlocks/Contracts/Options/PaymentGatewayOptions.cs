using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Options
{
    public class PaymentGatewayOptions
    {
        public const string SectionName = "PaymentGateways";

        [Required]
        [Url]
        public string CallbackBaseUrl { get; set; } = string.Empty;

        public ZarinPalOptions ZarinPal { get; set; } = new();
        public ZibalOptions Zibal { get; set; } = new();
        public SandboxOptions Sandbox { get; set; } = new();
    }

    public class ZarinPalOptions
    {
        [Required]
        public string MerchantId { get; set; } = string.Empty;

        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string CallbackUrl { get; set; } = string.Empty;

        [Required]
        public string BasePaymentUrl { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        [Range(1, 300)]
        public int Timeout { get; set; } = 30;
    }

    public class ZibalOptions
    {
        [Required]
        public string MerchantId { get; set; } = string.Empty;

        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string CallbackUrl { get; set; } = string.Empty;

        [Required]
        public string BasePaymentUrl { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        [Range(1, 300)]
        public int Timeout { get; set; } = 30;
    }

    public class SandboxOptions
    {
        public bool IsEnabled { get; set; }
        public bool AutoSuccess { get; set; } = true;

        [Range(0, 60)]
        public int DelaySeconds { get; set; } = 2;
    }
}
