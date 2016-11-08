using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class BTCeAPIException : Exception
    {
        public BTCeAPIException(string message) : base(message)
        {
        }
    }
}
