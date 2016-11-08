using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class TradeAnswer
    {
        public decimal Received { get; private set; }
        public decimal Remains { get; private set; }
        public int OrderId { get; private set; }
        public List<Currency> Currencies { get; private set; }

        private TradeAnswer() { }

        public static TradeAnswer ReadFromJSON(string answer)
        {
            JObject data = JObject.Parse(answer)["return"] as JObject;
            JValue success = JObject.Parse(answer)["success"] as JValue;
            JValue error = JObject.Parse(answer)["error"] as JValue;

            if ((int)success == 1)
            {
                var o = data["funds"].ToObject<Dictionary<string, decimal>>();

                List<Currency> currencies = new List<Currency>();

                foreach (string key in o.Keys)
                {
                    currencies.Add(new Currency(key, o[key]));
                }

                TradeAnswer tradeAnwer = new TradeAnswer()
                {
                    Currencies = currencies,
                    Received = data.Value<decimal>("received"),
                    Remains = data.Value<decimal>("remains"),
                    OrderId = data.Value<int>("order_id")
                };

                return tradeAnwer;
            }

            throw new BTCeAPIException((string)error);
        }
    }
}
    