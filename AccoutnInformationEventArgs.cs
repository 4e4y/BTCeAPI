using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class AccoutnInformationEventArgs : EventArgs
    {
        public int ChangeType { private set; get; }
        public List<BTCeCurrency> ChangedCurrencies { private set; get; }

        public AccoutnInformationEventArgs(int changeType, List<BTCeCurrency> changedCurrencies)
        {
            ChangeType = changeType;
            ChangedCurrencies = changedCurrencies;
        }
    }
}
