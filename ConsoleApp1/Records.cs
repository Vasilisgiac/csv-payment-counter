using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Κλάση για να αποθηκεύσουμε τις τιμές του csv αρχείου
    /// </summary>
    class Records
    {
        
            public string ACCOUNT_NUMBER { get; set; }
            public string DESCRIPTION { get; set; }
            public string PAYDATE { get; set; }
            public string PAY_AMOUNT { get; set; }
            public string BALANCE { get; set; }
            public string CURRENCY { get; set; }
        
    }
}
