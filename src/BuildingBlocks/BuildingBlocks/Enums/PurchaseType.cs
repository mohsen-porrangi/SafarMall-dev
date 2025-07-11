using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Enums
{
    /// <summary>
    /// Purchase type enumeration (duplicated from external service for internal use)
    /// </summary>
    public enum PurchaseType
    {
        FullWallet = 1,      // پرداخت کامل از کیف پول
        FullPayment = 2,     // پرداخت کامل از درگاه
        Mixed = 3,           // ترکیبی (کیف پول + درگاه)
        Credit = 4           // اعتباری (B2B)
    }
}
