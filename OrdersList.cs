using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTCeAPI
{
    public class OrdersList
    {
        public Dictionary<int, OrderInfo> List { get; private set; }

        private OrdersList()
        {
            List = new Dictionary<int, OrderInfo>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (int orderId in List.Keys)
            {
                sb.AppendLine(string.Format("ID: {0}; Order: {1}", orderId, List[orderId].ToString()));
            }

            return sb.ToString();
        }

        public static OrdersList ReadFromJSON(string orders)
        {
            JObject data = JObject.Parse(orders)["return"] as JObject;
            JValue success = JObject.Parse(orders)["success"] as JValue;
            JValue error = JObject.Parse(orders)["error"] as JValue;

            if ((int)success == 1)
            {
                var list = new OrdersList();

                foreach (var order in data)
                {
                    int orderId = int.Parse(order.Key.ToString());
                    OrderInfo orderInfo = OrderInfo.ReadFromJSON(order.Value.ToString(), orderId);
                    list.List.Add(key: orderId, value: orderInfo);
                }

                return list;
            }

            throw new BTCeAPIException((string)error);
        }
    }
}
