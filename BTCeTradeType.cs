using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public enum BTCeTradeType
    {
        Sell,
        Buy
    }
    public class TradeTypeHelper
    {
        public static BTCeTradeType FromString(string s)
        {
            switch (s)
            {
                case "sell":
                    return BTCeTradeType.Sell;
                case "buy":
                    return BTCeTradeType.Buy;
                default:
                    throw new ArgumentException();
            }
        }
        public static string ToString(BTCeTradeType v)
        {
            return Enum.GetName(typeof(BTCeTradeType), v).ToLowerInvariant();
        }
    }
}
