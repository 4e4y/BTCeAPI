using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public enum BTCeCurrency
    {
        USD,
        LTC,
        BTC,
        NMC,
        RUR,
        EUR,
        NVC,
        TRC,
        PPC,
        FTC,
        XPN,
        CNH,
        GBP,
        DSH,
        ETH,
        Unknown
    }

    public class BTCeCurrencyHelper
    {
        public static BTCeCurrency FromString(string s)
        {
            BTCeCurrency ret = BTCeCurrency.Unknown;
            Enum.TryParse<BTCeCurrency>(s.ToLowerInvariant(), out ret);
            return ret;
        }

        public static string ToString(BTCeCurrency v)
        {
            return Enum.GetName(typeof(BTCeCurrency), v).ToLowerInvariant();
        }
    }
}
