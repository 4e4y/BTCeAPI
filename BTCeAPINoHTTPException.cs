using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class BTCeAPINoHTTPException : BTCeAPIException
    {
        public BTCeAPINoHTTPException()
            : base("Non HTTP WebRequest")
        {
        }
    }
}
