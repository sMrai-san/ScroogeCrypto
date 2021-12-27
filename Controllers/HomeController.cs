using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScroogeCrypto.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ScroogeCrypto.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GetCrypto(CryptoDateModel cModel)
        {
            if (!ModelState.IsValid)
            {
                return View(cModel);
            }

            if (cModel.EndDate < cModel.StartDate)
            {
                ModelState.AddModelError(nameof(cModel.EndDate),"End date must be further than start date!");
                return View(cModel);
            }

            var sDate = ((DateTimeOffset)cModel.StartDate).ToUnixTimeSeconds();
            //we have to add 1 day to get the last day's data too
            var eDate = ((DateTimeOffset)cModel.EndDate.AddDays(1)).ToUnixTimeSeconds();



            List<CryptoJsonModel> getCryptos = new List<CryptoJsonModel>();

            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            string cryptoJSON = client.DownloadString("https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency=eur&from=" + sDate + "&to=" + eDate);
            //CryptoJsonModel setCryptos = JsonConvert.DeserializeObject<CryptoJsonModel>(cryptoJSON);
            //var priceList = setCryptos.prices.ToList();
            var prices= JsonConvert.DeserializeObject<CryptoJsonModel>(cryptoJSON).prices;

            double mostValuable = 0;
            DateTime mostValuableDate = DateTime.Today;
            double priceHolder = 0;
            int downwardHolder = 0;
            DateTime currentDay = UnixTimeToDateTime((long)prices[0][0]).Date;
            List<int> downwardDaysList = new List<int>();

            foreach (var item in prices)
            {

                DateTime itemDate = UnixTimeToDateTime((long)item[0]).Date;
                //We are checking the first price in current date, other prices on that date we will not check here
                if (currentDay.Date == itemDate)
                {
                    if (priceHolder > item[1])
                    {
                        downwardHolder++;
                    }
                    else if (priceHolder < item[1])
                    {
                        downwardDaysList.Add(downwardHolder);
                        downwardHolder = 0;
                    }
                    CryptoJsonModel m = new CryptoJsonModel();
                    m.Date = UnixTimeToDateTime((long)item[0]);
                    m.Price = item[1];
                    getCryptos.Add(m);
                    currentDay = UnixTimeToDateTime((long)item[0]);
                    currentDay = currentDay.AddDays(1);
                    //this item price will be written in variable
                    priceHolder = item[1];

                    if (priceHolder > mostValuable)
                    {
                        mostValuable = priceHolder;
                        mostValuableDate = UnixTimeToDateTime((long)item[0]);
                    }
                }
            }

            //The maximum amount of days bitcoin’s price was decreasing in a row
            string longestDownward = downwardDaysList.Max().ToString();
            ViewData["Downward"] = longestDownward;

            //highest trading volume within a given date range (rounded value is commented)
            //ViewData["HighestPrice"] = Math.Round(mostValue, 2).ToString();
            ViewData["HighestPrice"] = mostValuable;
            //If you need just the date:
            //ViewData["HighestPriceDate"] = mostValuableDate.ToString("dd/MM/yyyy");
            ViewData["HighestPriceDate"] = mostValuableDate;


            return View(getCryptos);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixtime).ToLocalTime();
            return dtDateTime;
        }
    }
}
