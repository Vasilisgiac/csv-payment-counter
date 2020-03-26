using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Κλάση για τις καταγραφές που είναι έτοιμες να εκχωρηθούν στην βάση
    /// </summary>
    class Records2
    {
        public long ACCOUNT_NUMBER { get; set; }
        public string DESCRIPTION { get; set; }
        public DateTime PAY_DATE { get; set; }
        public decimal PAY_AMOUNT { get; set; }
        public decimal BALANCE { get; set; }
        public string CURRENCY { get; set; }
        public decimal BALANCE_CURRENCY { get; set; }
        public decimal PAY_AMOUNT_CURRENCY { get; set; }
    }
}
