using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class DecimalEventArgs : EventArgs
    {
        public decimal Value { private set; get; }

        public DecimalEventArgs() : base()
        {
            Value = -1;
        }

        public DecimalEventArgs(decimal value)
        {
            Value = value;
        }
    }
}
