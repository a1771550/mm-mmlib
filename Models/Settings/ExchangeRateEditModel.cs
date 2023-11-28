using CommonLib.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MMDAL;
using CommonLib.Models;
using RestSharp;
using System.Net;
using System.Configuration;
using System.Threading.Tasks;
using System;
using System.Text.Json;

namespace MMLib.Models.POS.Settings
{
    public class ExchangeRateEditModel
    {
        private ComInfo ComInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        private static ComInfo comInfo { get { return HttpContext.Current.Session["ComInfo"] as ComInfo; } }
        public bool UseForexAPI { get; set; } = false;

        public ExchangeRateEditModel()
        {
            //MUST NOT use Session ComInfo here! use DB data instead!!!
            using var context = new MMDbContext();
            UseForexAPI = GetForexInfo(context);
        }

        public static bool GetForexInfo(MMDbContext context)
        {           
            var useapi = context.ComInfoes.FirstOrDefault(x => x.AccountProfileId == comInfo.AccountProfileId).UseForexAPI;
            return useapi==null?false:(bool)useapi;
        }

        public Dictionary<string, string> ExRateList
        {
            get
            {
                using var context=new MMDbContext();
                var list = new Dictionary<string, string>();

                var exchangerates = context.MyobCurrencies.Where(x => x.AccountProfileId == ComInfo.AccountProfileId).ToList();
                if(exchangerates!=null && exchangerates.Count > 0)
                {
                    foreach(var exrate in exchangerates)
                    {
                        list[exrate.CurrencyCode] = exrate.ExchangeRate.ToString();
                    }
                }
                else
                {
                    var comInfo = context.ComInfoes.FirstOrDefault(x => x.Id == ComInfo.Id && x.AccountProfileId == ComInfo.AccountProfileId);
                    foreach (var exrate in comInfo.ExchangeRates.Split(';'))
                    {
                        var arr = exrate.Split(':');
                        var currencycode = arr[0];
                        list[currencycode] = arr[1];
                    }
                } 
                return list;
            }
        }

        public async Task<Dictionary<string, string>> GetExRateList()
        {

            var list = new Dictionary<string, string>();

            //if (_ComInfo.ExchangeRates == null)
            //{                    
            Dictionary<string, decimal> exchangeRateToEuro = CommonHelper.GetExchangeRateToEuro();
            string[] currencyoptions = ComInfo.StockInCurrencyOptions.Split(',');
            foreach (var co in currencyoptions)
            {
                list[co] = CommonHelper.GetExRate(exchangeRateToEuro, co);
            }
            var client = new RestClient("https://api.apilayer.com/currency_data/live?source=MOP&currencies=HKD");
            var request = new RestRequest("https://api.apilayer.com/currency_data/live?source=MOP&currencies=HKD", Method.Get);
            request.AddHeader("apikey", ConfigurationManager.AppSettings["CurrencyApiKey"]);
            RestResponse response = await client.ExecuteAsync(request);
            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                CurrencyAPI currencyAPI = JsonSerializer.Deserialize<CurrencyAPI>(response.Content);
                list["MOP"] = currencyAPI.quotes.MOPHKD.ToString();
            }
            return list;

        }
        public static void Save(Dictionary<string, decimal> model, int useapi, int apId)
        {
            using var context = new MMDbContext();
            List<MyobCurrency> myobCurrencies = context.MyobCurrencies.Where(x => x.AccountProfileId == apId).ToList();
            var cominfo = context.ComInfoes.FirstOrDefault(x => x.Id == comInfo.Id);
            //cominfo.ExchangeRates = string.Join(";", model.Select(x => x.Key + ":" + x.Value).ToArray());
            cominfo.UseForexAPI = useapi == 1;
            cominfo.ModifyTime = DateTime.Now;
            List<MyobCurrency> newforexes = new List<MyobCurrency>();
            foreach(var item in model)
            {
                var forex = myobCurrencies.FirstOrDefault(x => x.CurrencyCode == item.Key);
                if (forex!=null){
                    forex.ExchangeRate = Convert.ToDouble(item.Value);
                    forex.ModifyTime = DateTime.Now;
                }
                else
                {
                    var currency = myobCurrencies.FirstOrDefault();
                    var newcurrId = myobCurrencies.Max(x => x.CurrencyID)+1;
                    newforexes.Add(new MyobCurrency
                    {
                        CurrencyID=newcurrId,
                        CurrencyCode=item.Key,
                        ExchangeRate=Convert.ToDouble(item.Value),        
                        SymbolPosition=currency.SymbolPosition,
                        DecimalPlaces=currency.DecimalPlaces,
                        NumberDigitsInGroup = currency.NumberDigitsInGroup,
                        NegativeFormat=currency.NegativeFormat,
                        UseLeadingZero=currency.UseLeadingZero,                        
                        AccountProfileId = apId,
                        CreateTime = DateTime.Now,
                    });
                }
            }
            if (newforexes.Count > 0)
            {
                context.MyobCurrencies.AddRange(newforexes);
                context.SaveChanges();
            }
                
            context.SaveChanges();
        }
    }

    public class ExchangeRateModel
    {
        public Dictionary<string, decimal> ExList { get; set; }
        public string Currency { get; set; }
        public decimal ExRate { get; set; }         
    }
}
