using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Κλάση για να εκτυπώσουμε τις πληρωμές
    /// </summary>
    class Payments
    {
        public string CURRENCY { get; set; }
        public decimal PAY_AMOUNT { get; set; }

        public override string ToString()
        {
            return CURRENCY + "  Total amount of payments in EURO: " + PAY_AMOUNT;
        }
    }
    
}
