using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScroogeCrypto.Models
{
    public class CryptoProfitModel
    {
        public DateTime BuyDate { get; set; }
        public DateTime SellDate { get; set; }
        public double Profit { get; set; }

        public string Error { get; set; }

    }
}
