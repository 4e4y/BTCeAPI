using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public enum BTCeOrderStatus
    {
        Active,
        Executed,
        Canceled,
        CanceledPartiallyExecuted,
        Unknown
    }
    public class OrderStatusHelper
    {
        public static BTCeOrderStatus FromString(string s)
        {
            switch (s.ToLower())
            {
                case "active":
                    return BTCeOrderStatus.Active;
                case "executed":
                    return BTCeOrderStatus.Executed;
                case "canceled":
                    return BTCeOrderStatus.Canceled;
                case "canceledpartiallyexecuted":
                    return BTCeOrderStatus.CanceledPartiallyExecuted;
                default:
                    return BTCeOrderStatus.Unknown;
            }
        }
        public static string ToString(BTCeOrderStatus v)
        {
            return Enum.GetName(typeof(BTCeOrderStatus), v).ToLowerInvariant();
        }
    }
}
