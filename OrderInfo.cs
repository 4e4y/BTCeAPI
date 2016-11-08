using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class OrderInfo
    {
        public int Id { get; private set; }
        public BTCePair Pair { get; private set; }
        public BTCeTradeType Type { get; private set; }
        public decimal Amount { get; private set; }
        public decimal Rate { get; private set; }
        public UInt32 TimestampCreated { get; private set; }
        public BTCeOrderStatus Status { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Pair: {0}; Type: {1}; Amount: {2}; Rate: {3}; TimestampCreated: {4}; Status: {5}", Pair, Type, Amount, Rate, TimestampCreated, Status);

            return sb.ToString();
        }
        public static OrderInfo ReadFromJSON(string order, int orderId)
        {
            JObject o = JObject.Parse(order) as JObject;
            if (o == null)
            {
                return null;
            }

            OrderInfo orderInfo = new OrderInfo()
            {
                Pair = BtcePairHelper.FromString(o.Value<string>("pair")),
                Type = TradeTypeHelper.FromString(o.Value<string>("type")),
                Amount = o.Value<decimal>("amount"),
                Rate = o.Value<decimal>("rate"),
                TimestampCreated = o.Value<UInt32>("timestamp_created"),
                Status = (BTCeOrderStatus)o.Value<int>("status"),
                Id = orderId
            };

            return orderInfo;
        }
    }
}
