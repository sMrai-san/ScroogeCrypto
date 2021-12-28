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
                ModelState.AddModelError(nameof(cModel.EndDate), "End date must be further than start date!");
                return View(cModel);
            }

            var sDate = ((DateTimeOffset)cModel.StartDate).ToUnixTimeSeconds();
            //we can add 1 day here to get the last day's data too
            var eDate = ((DateTimeOffset)cModel.EndDate.AddDays(1)).ToUnixTimeSeconds();

            //************************************************************
            //Getting the bitcoin prices from API (range)
            //************************************************************
            List<CryptoJsonModel> getCryptos = new List<CryptoJsonModel>();
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            string cryptoJSON = client.DownloadString("https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency=eur&from=" + sDate + "&to=" + eDate);
            //CryptoJsonModel setCryptos = JsonConvert.DeserializeObject<CryptoJsonModel>(cryptoJSON);
            //var priceList = setCryptos.prices.ToList();
            var prices = JsonConvert.DeserializeObject<CryptoJsonModel>(cryptoJSON).prices;

            double firstPriceInCryptoList = prices[0][1];
            DateTime firstDateInCryptoList = UnixTimeToDateTime((long)prices[0][0]).Date;

            double mostValuable = firstPriceInCryptoList;
            DateTime mostValuableDate = firstDateInCryptoList;
            double leastValuable = prices[0][1];
            DateTime leastValuableDate = firstDateInCryptoList;
            double currentDayPriceHolder = firstPriceInCryptoList;
            int downwardHolder = 0;
            DateTime currentDay = firstDateInCryptoList;
            List<int> downwardDaysList = new List<int>();
            int daysInRange = 0;

            List<CryptoJsonModel> timeMachine = new List<CryptoJsonModel>();
            List<double> kk = new List<double>();

            //Looping each item from the prices -list
            foreach (var item in prices)
            {
                DateTime itemDate = UnixTimeToDateTime((long)item[0]).Date;
                //We are checking the first price in current date, other prices on that date we will not check here
                if (currentDay.Date == itemDate)
                {
                    //Check if the price is going downward, if true, +1 to a downward counter
                    if (currentDayPriceHolder > item[1])
                    {
                        downwardHolder++;
                    }
                    //Otherwise add the downward counter to list (may be zero) and reset counter
                    else if (currentDayPriceHolder < item[1])
                    {
                        downwardDaysList.Add(downwardHolder);
                        downwardHolder = 0;
                        ////When the price goes up, we want to buy the day before! (Adding that day and price to the list)
                        //CryptoJsonModel tb = new CryptoJsonModel();
                        //var dayToAdd = UnixTimeToDateTime((long)item[0]);
                        //tb.Date = dayToAdd.AddDays(-1);
                        //tb.Price = currentDayPriceHolder;
                        //timeMachine.Add(tb);
                    }

                    CryptoJsonModel m = new CryptoJsonModel();
                    m.Date = UnixTimeToDateTime((long)item[0]);
                    m.Price = item[1];
                    getCryptos.Add(m);
                    kk.Add(item[1]);
                    currentDay = UnixTimeToDateTime((long)item[0]);
                    currentDay = currentDay.AddDays(1);
                    currentDayPriceHolder = item[1];
                    daysInRange++;

                    //Check if the current day price is most valuable
                    if (currentDayPriceHolder > mostValuable)
                    {
                        mostValuable = currentDayPriceHolder;
                        mostValuableDate = UnixTimeToDateTime((long)item[0]);
                    }
                    //Check if the current day price is least valuable
                    if (leastValuable > currentDayPriceHolder)
                    {
                        leastValuable = currentDayPriceHolder;
                        leastValuableDate = UnixTimeToDateTime((long)item[0]);
                    }
                }
            }
            //Let's add the ongoing count into our list
            downwardDaysList.Add(downwardHolder);

            //A
            //The maximum amount of days bitcoin’s price was decreasing in a row
            ViewData["Downward"] = downwardDaysList.Max();

            //B
            //highest trading volume within a given date range (rounded value is commented)
            //ViewData["HighestPrice"] = Math.Round(mostValue, 2).ToString();
            ViewData["HighestPrice"] = mostValuable;
            //If you need just the date:
            //ViewData["HighestPriceDate"] = mostValuableDate.ToString("dd/MM/yyyy");
            ViewData["HighestPriceDate"] = mostValuableDate;

            //C
            //Least valuable date
            //ViewData["LowestPrice"] = Math.Round(leastValuable, 2).ToString();
            ViewData["LowestPrice"] = leastValuable;
            //ViewData["LowestPriceDate"] = leastValuableDate.ToString("dd/MM/yyyy");
            ViewData["LowestPriceDate"] = leastValuableDate;

            //If the count is same in both days in range / downward days (excluding first day in range)
            if (daysInRange - 1 == downwardDaysList.Max())
            {
                ViewData["DoNotBuy"] = "Given date range gave a downward trend, do not buy (back to the future)!";
            }

            
            var kikkelipaa = MaxProfit(getCryptos);
            ViewData["TimeToSell"] = kikkelipaa;

            return View(getCryptos);
        }

        public List<string> MaxProfit(List<CryptoJsonModel> pricesList)
        {
            List<double> priceList = new List<double>();
            foreach (var item in pricesList)
            {
                priceList.Add((double)item.Price);
            }

            int n = priceList.ToArray().Length;

            List<string> daysToBuyAndSell = new List<string>();
            // Prices must be given for at least two days
            if (n == 1)
            {
                daysToBuyAndSell.Add("Prices must be given for at least two days");
                return daysToBuyAndSell;
            }

            int count = 0;

            // solution array
            List<TimeMachine> sol = new List<TimeMachine>();

            // Traverse through given price array
            int i = 0;
            while (i < n - 1)
            {
                // Find Local Minima. Note that
                // the limit is (n-2) as we are
                // comparing present element
                // to the next element.
                while ((i < n - 1) && (priceList[i + 1] <= priceList[i]))
                    i++;

                // If we reached the end, break
                // as no further solution possible
                if (i == n - 1)
                    break;

                TimeMachine e = new TimeMachine();
                e.Buy = i++;
                // Store the index of minima

                // Find Local Maxima. Note that
                // the limit is (n-1) as we are
                // comparing to previous element
                while ((i < n) && (priceList[i] >= priceList[i - 1]))
                    i++;

                // Store the index of maxima
                e.Sell = i - 1;
                sol.Add(e);

                // Increment number of buy/sell
                count++;
            }

            //
            if (count == 0)
            {
                daysToBuyAndSell.Add("Given date range gave a downward trend, do not buy (back to the future)!");
            }
            else
            {
                for (int j = 0; j < count; j++)
                {
                    var buyDate = pricesList.ElementAt(Convert.ToInt32(sol[j].Buy));
                    var sellDate = pricesList.ElementAt(Convert.ToInt32(sol[j].Sell));
                    daysToBuyAndSell.Add("Buy on day: " + buyDate.Date.ToString() + " Sell on day: " + sellDate.Date.ToString());
                }
            }

            return daysToBuyAndSell;
        }

        //Converter Function (UnixTimo to DateTime)
        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixtime).ToLocalTime();
            return dtDateTime;
        }

        //Default error management
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
