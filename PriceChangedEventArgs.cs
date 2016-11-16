using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class PriceChangedEventArgs : EventArgs
    {
        public int PriceChangedIndicator { private set; get; }

        public PriceChangedEventArgs()
        {
            PriceChangedIndicator = -1;
        }

        public PriceChangedEventArgs(int priceChangedIndicator)
        {
            PriceChangedIndicator = priceChangedIndicator;
        }
    }
}
