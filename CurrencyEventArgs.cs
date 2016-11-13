using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class CurrencyEventArgs : EventArgs
    {
        public List<BTCeCurrency> Currencies { private set; get; }

        public CurrencyEventArgs()
        {
            Currencies = new List<BTCeCurrency>();
            Currencies.Add(BTCeCurrency.Unknown);
        }

        public CurrencyEventArgs(List<BTCeCurrency> currencies)
        {
            Currencies = currencies;
        }
    }
}
