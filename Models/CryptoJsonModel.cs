using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScroogeCrypto.Models
{
    public class CryptoJsonModel
    {
        public List<List<double>> prices { get; set; }
        public List<List<double>> market_caps { get; set; }
        public List<List<double>> total_volumes { get; set; }

        public double? Price { get; set; }
        public DateTime? Date { get; set; }

    }
}
