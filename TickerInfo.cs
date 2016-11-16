using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class TickerInfo
    {
        public decimal Average { get; private set; }
        public decimal Buy { get; private set; }
        public decimal High { get; private set; }
        public decimal Last { get; private set; }
        public decimal Low { get; private set; }
        public decimal Sell { get; private set; }
        public decimal Volume { get; private set; }
        public decimal VolumeCurrency { get; private set; }
        public UInt32 ServerTime { get; private set; }

        public static TickerInfo ReadFromJSON(string ticker)
        {
            if (string.IsNullOrEmpty(ticker))
                return null;

            JObject o = JObject.Parse(ticker)["ticker"] as JObject;

            return new TickerInfo()
            {
                Average = o.Value<decimal>("avg"),
                Buy = o.Value<decimal>("buy"),
                High = o.Value<decimal>("high"),
                Last = o.Value<decimal>("last"),
                Low = o.Value<decimal>("low"),
                Sell = o.Value<decimal>("sell"),
                Volume = o.Value<decimal>("vol"),
                VolumeCurrency = o.Value<decimal>("vol_cur"),
                ServerTime = o.Value<UInt32>("server_time"),
            };
        }

        public override string ToString()
        {
            return string.Format("Average: {0}, Buy: {1}, High: {2}, Last: {3}, Low: {4}, Sell: {5}, Volume: {6}, VolumeCurrent: {7}, Sell: {8}", Average, Buy, High, Last, Low, Sell, Volume, VolumeCurrency, Sell);
        }
    }
}
