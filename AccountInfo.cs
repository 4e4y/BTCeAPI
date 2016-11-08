using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class Currency
    {
        public string Name { get; private set; }
        public decimal Value { get; private set; }

        public Currency(string name, decimal value)
        {
            Name = name;
            Value = value;
        }
    }

    public class Rights
    {
        public bool Info { get; private set; }
        public bool Trade { get; private set; }

        public static Rights ReadFromJSON(JObject o)
        {
            if (o == null)
            {
                return null;
            }

            return new Rights()
            {
                Info = o.Value<int>("info") == 1,
                Trade = o.Value<int>("trade") == 1
            };
        }
    }

    public class AccountInfo
    {
        public List<Currency> Currencies { get; private set; }
        public Rights Rights { get; private set; }
        public int TransactionsCount { get; private set; }
        public int OpenOrders { get; private set; }
        public int ServerTime { get; private set; }

        private AccountInfo() { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("Funds: ");
            foreach (Currency c in Currencies)
            {
                if (c.Value > 0)
                {
                    sb.AppendLine(string.Format("{0} -> {1}", c.Name, c.Value));
                }
            }

            sb.AppendLine(string.Format("Rights: Info: {0}; Trade: {1}", Rights.Info, Rights.Trade));

            sb.AppendLine(string.Format("Transactions Count: {0}", TransactionsCount));
            sb.AppendLine(string.Format("Open Orders: {0}", OpenOrders));

            return sb.ToString();
        }

        public static AccountInfo ReadFromJSON(string info)
        {
            // var funds = Funds.ReadFromJObject(o["funds"] as JObject);

            JObject data = JObject.Parse(info)["return"] as JObject;
            JValue success = JObject.Parse(info)["success"] as JValue;
            JValue error = JObject.Parse(info)["error"] as JValue;

            if ((int)success == 1)
            {
                var o = data["funds"].ToObject<Dictionary<string, decimal>>();

                List<Currency> currencies = new List<Currency>();

                foreach (string key in o.Keys)
                {
                    currencies.Add(new Currency(key, o[key]));
                }

                var userInfo = new AccountInfo()
                {
                    // Funds = funds,
                    Currencies = currencies,
                    Rights = Rights.ReadFromJSON(data["rights"] as JObject),
                    TransactionsCount = data.Value<int>("transaction_count"),
                    OpenOrders = data.Value<int>("open_orders"),
                    ServerTime = data.Value<int>("server_time")
                };
                return userInfo;
            }

            throw new BTCeAPIException((string)error);
        }
    }
}
