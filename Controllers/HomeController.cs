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
        //*****************************************************
        //FRONTPAGE (includes inputs and help)
        //*****************************************************
        public IActionResult Index()
        {
            return View();
        }

        //*****************************************************
        //FRONTPAGE POST
        //*****************************************************
        [HttpPost]
        public IActionResult GetCrypto(CryptoDateModel cModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", cModel);
            }
            //End date must be further than start date
            if (cModel.EndDate < cModel.StartDate)
            {
                ModelState.AddModelError(nameof(cModel.EndDate), "End date must be further than start date!");
                return View("Index", cModel);
            }
            else
            {
                //We need to convert datetime to unixtime here
                var sDate = ((DateTimeOffset)cModel.StartDate).ToUniversalTime().ToUnixTimeSeconds();
                //We can add 1 day here to get the last day's data too
                var eDate = ((DateTimeOffset)cModel.EndDate.AddDays(1)).ToUniversalTime().ToUnixTimeSeconds();

                //************************************************************
                //Getting the bitcoin prices from API (range)
                //************************************************************
                List<CryptoJsonModel> getCryptos = new List<CryptoJsonModel>();
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                string cryptoJSON = client.DownloadString("https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency=eur&from=" + sDate + "&to=" + eDate);
                var prices = JsonConvert.DeserializeObject<CryptoJsonModel>(cryptoJSON).prices;

                //If we have no data from the API or we get errors...
                try
                {
                    double firstPriceInCryptoList = prices[0][1];
                    DateTime firstDateInCryptoList = UnixTimeToDateTime((long)prices[0][0]).Date;

                    double mostValuable = firstPriceInCryptoList;
                    DateTime mostValuableDate = firstDateInCryptoList;
                    double leastValuable = prices[0][1];
                    DateTime leastValuableDate = firstDateInCryptoList;

                    double currentDayPriceHolder = firstPriceInCryptoList;
                    DateTime currentDay = firstDateInCryptoList;

                    int downwardHolder = 0;
                    List<int> downwardDaysList = new List<int>();

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
                            }

                            CryptoJsonModel m = new CryptoJsonModel();
                            m.Date = UnixTimeToDateTime((long)item[0]);
                            m.Price = item[1];
                            getCryptos.Add(m);
                            currentDay = UnixTimeToDateTime((long)item[0]);
                            currentDay = currentDay.AddDays(1);
                            currentDayPriceHolder = item[1];

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
                    ViewData["HighestPriceDate"] = mostValuableDate.ToString("dd.MM.yyyy HH:mm:ss");

                    //C
                    //Least valuable date
                    //ViewData["LowestPrice"] = Math.Round(leastValuable, 2).ToString();
                    ViewData["LowestPrice"] = leastValuable;
                    //ViewData["LowestPriceDate"] = leastValuableDate.ToString("dd/MM/yyyy");
                    ViewData["LowestPriceDate"] = leastValuableDate.ToString("dd.MM.yyyy HH:mm:ss");

                    var getProfits = MaxProfit(getCryptos);
                    var nullProfits = getProfits.FirstOrDefault();
                    if (nullProfits.Error == "1")
                    {

                        ViewData["NoTimeToBuyOrSell"] = "Prices must be given for at least two days for timemachine to work correctly.";
                    }
                    else if (nullProfits.Error == "2")
                    {
                        ViewData["NoTimeToBuyOrSell"] = "Given date range gave a downward trend, do not buy (back to the future)!";
                    }
                    else
                    {
                        ViewData["TimeToSell"] = getProfits;
                    }
                    return View(getCryptos);
                }
                //...we go back to the Index View and try again
                catch
                {
                    ModelState.AddModelError(nameof(cModel.StartDate), "Something went wrong when fetching data from API, please check your dates and try again!");
                    return View("Index", cModel);
                }
            }

        }

        //*********************************************************************
        //HELPER FUNCTIONS
        //*********************************************************************

        //************************************************************
        //Helper function to get the days when it's good to sell / buy
        //https://www.geeksforgeeks.org/stock-buy-sell/
        //************************************************************
        public List<CryptoProfitModel> MaxProfit(List<CryptoJsonModel> pricesList)
        {
            //for data and counting purposes
            List<double> priceList = new List<double>();
            //helper list for saving data
            List<CryptoProfitModel> cProfit = new List<CryptoProfitModel>();
            //return data goes here
            List<CryptoProfitModel> daysToBuyAndSell = new List<CryptoProfitModel>();

            foreach (var item in pricesList)
            {
                priceList.Add((double)item.Price);
            }

            //List to Array to get the lenght
            int n = priceList.ToArray().Length;

            //Prices must be given for at least two days
            if (n == 1)
            {
                CryptoProfitModel cProfitModel = new CryptoProfitModel();
                cProfitModel.BuyDate = DateTime.Now;
                cProfitModel.SellDate = DateTime.Now;
                cProfitModel.Profit = 0;
                cProfitModel.Error = "1";
                daysToBuyAndSell.Add(cProfitModel);
                return daysToBuyAndSell;
            }

            int count = 0;

            //Solution list (using almost the same variables than example code)
            List<TimeMachineModel> sol = new List<TimeMachineModel>();

            //Looping the priceList
            int i = 0;
            while (i < n - 1)
            {
                //Find Local Minima.
                while ((i < n - 1) && (priceList[i + 1] <= priceList[i]))
                    i++;

                //If we reached the end, break
                if (i == n - 1)
                    break;

                TimeMachineModel e = new TimeMachineModel();
                //Store the index of minima
                e.Buy = i++;

                //Find Local Maxima.
                while ((i < n) && (priceList[i] >= priceList[i - 1]))
                    i++;

                //Store the index of maxima
                e.Sell = i - 1;
                sol.Add(e);

                //Increment number of buy/sell
                count++;
            }

            //If all the days are downward...
            if (count == 0)
            {
                CryptoProfitModel cProfitModel = new CryptoProfitModel();
                cProfitModel.BuyDate = DateTime.Now;
                cProfitModel.SellDate = DateTime.Now;
                cProfitModel.Profit = 0;
                cProfitModel.Error = "2";
                daysToBuyAndSell.Add(cProfitModel);
            }
            else
            {
                //********************************************************************
                //Getting the best day to buy and best day to sell into a custom model
                //HOX! it is also possible to see all the best days within the range (see commented code below)
                for (int j = 0; j < count; j++)
                {
                    CryptoProfitModel cProfitModel = new CryptoProfitModel();
                    var buyDate = pricesList.ElementAt(Convert.ToInt32(sol[j].Buy));
                    var sellDate = pricesList.ElementAt(Convert.ToInt32(sol[j].Sell));
                    cProfitModel.BuyDate = (DateTime)buyDate.Date;
                    cProfitModel.SellDate = (DateTime)sellDate.Date;
                    cProfitModel.Profit = (double)(sellDate.Price - buyDate.Price);
                    cProfit.Add(cProfitModel);

                    //All the days when it's good to buy/sell
                    //daysToBuyAndSell.Add(cProfitModel);
                }
                //The best day for selling the bought bitcoin to maximize profits
                var maxProfit = cProfit.Where(x => x.Profit == cProfit.Max(x => x.Profit)).FirstOrDefault();
                daysToBuyAndSell.Add(maxProfit);
            }
            return daysToBuyAndSell;
        }

        //**********************************************
        //Helper Function Converter (UnixTime to DateTime)
        //**********************************************
        public DateTime UnixTimeToDateTime(long unixtime)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixtime).ToLocalTime();
            return dtDateTime;
        }



        //*********************************************************************
        //ASP.NET DEFAULT ERROR STUFF
        //*********************************************************************
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
