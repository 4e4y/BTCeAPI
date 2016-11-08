using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public enum BTCePair
    {
        btc_usd,
        btc_rur,
        btc_eur,
        ltc_btc,
        ltc_usd,
        ltc_rur,
        ltc_eur,
        /*nmc_btc,
        nmc_usd,
        nvc_btc,
        nvc_usd,*/
        usd_rur,
        eur_usd,
        /*trc_btc,
        ppc_btc,
        ppc_usd,
        ftc_btc,
        xpm_btc,*/
        Unknown
    }

    public class BtcePairHelper
    {
        public static BTCePair FromString(string s)
        {
            BTCePair ret = BTCePair.Unknown;
            Enum.TryParse<BTCePair>(s.ToLowerInvariant(), out ret);
            return ret;
        }

        public static string ToString(BTCePair v)
        {
            return Enum.GetName(typeof(BTCePair), v).ToLowerInvariant();
        }

        public static string ToBetterString(BTCePair v)
        {
            return Enum.GetName(typeof(BTCePair), v).ToLowerInvariant().ToUpper().Replace('_', '/');
        }

        public static string ToBetterString(string v)
        {
            return v.ToLowerInvariant().ToUpper().Replace('_', '/');
        }

        public static BTCePair FromBetterString(string s)
        {
            BTCePair ret = BTCePair.Unknown;
            Enum.TryParse<BTCePair>(s.ToLowerInvariant().Replace('/', '_'), out ret);
            return ret;
        }
    }

}
