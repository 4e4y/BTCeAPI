using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class FeeInfo
    {
        public decimal Fee { get; private set; }

        public static FeeInfo ReadFromJSON(string fee)
        {
            if (string.IsNullOrEmpty(fee))
                return null;

            JValue o = JObject.Parse(fee)["trade"] as JValue;

            return new FeeInfo()
            {
                Fee = (decimal)o
            };
        }

        public override string ToString()
        {
            return string.Format("Fee: {0}", Fee);
        }
    }
}
