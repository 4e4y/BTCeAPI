using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public enum AccountInfoChangeType
    {
        /// <summary>
        /// Account Information is received for the first time
        /// </summary>
        FIRST_TIME_CALL = 1,

        /// <summary>
        ///  Open Orders count changed
        /// </summary>
        OPEN_ORDERS_CHANGED = 2,

        /// <summary>
        /// Any change in the available Currencies
        /// As part of additional Event infromation List with Currencies will be send
        /// </summary>
        CURRENCY_AMOUNT_CHANGED = 4,

        /// <summary>
        /// Any change in Account Rights
        /// </summary>
        RIGHTS_CHANGED = 8,

        /// <summary>
        /// There is an exception thrown during getting Account Information
        /// </summary>
        EXCEPTION = 16
    }
}
