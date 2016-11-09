using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Web;

namespace BTCeAPI
{
    static class UnixTime
    {
        static DateTime unixEpoch;
        static UnixTime()
        {
            unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static UInt32 Now { get { return GetFromDateTime(DateTime.UtcNow); } }
        public static UInt32 GetFromDateTime(DateTime d) { return (UInt32)(d - unixEpoch).TotalSeconds; }
        public static DateTime ConvertToDateTime(UInt32 unixtime) { return unixEpoch.AddSeconds(unixtime); }
    }

    public class BTCeAPIWrapper
    {
        #region Static Members

        private static ILog logger = log4net.LogManager.GetLogger("Console");

        private static BTCeAPIWrapper instance;
        private static object lockObject = new object();

        private static string BTCeAPITickerURL = "https://btc-e.com/api/2/{0}/ticker";
        private static string BTCeAPIFeeURL = "https://btc-e.com/api/2/{0}/fee";
        private static string BTCeAPIPrivateURL = "https://btc-e.com/tapi";
        public static UInt32 Nonce = UnixTime.Now;
        private static UInt32 lastNonce = 0;

        #endregion Static Members

        #region Private members

        private Timer Ticker = new Timer();
        private Timer Fee = new Timer();
        private Timer ActiveOrders = new Timer();
        private Timer Info = new Timer();

        private int tickerTimeout = 1;
        private int feeTimeout = 259200;
        private int ordersTimeout = 5;
        private int infoTimeout = 2;
        private BTCePair currency = BTCePair.btc_usd;

        private string key;
        private HMACSHA512 hashMaker;

        private bool startActiveOtrders = false;
        private bool authenticated = false;

        #endregion Private members

        #region Public Members

        public EventHandler PriceChanged;
        public EventHandler FeeChanged;
        public EventHandler InfoReceived;
        public EventHandler ActiveOrdersReceived;

        #endregion Public Members

        #region Properties

        public int TickerTimeout { private get { return tickerTimeout; } set { Ticker.Stop(); tickerTimeout = value; Ticker.Interval = tickerTimeout * 1000; Ticker.Start(); } }
        public int FeeTimeout { private get { return feeTimeout; } set { Fee.Stop(); feeTimeout = value; Fee.Interval = feeTimeout * 1000; Fee.Start(); } }
        public int OrdersTimeout { private get { return ordersTimeout; } set { ActiveOrders.Stop(); ordersTimeout = value; ActiveOrders.Interval = ordersTimeout * 1000; if (startActiveOtrders) { ActiveOrders.Start(); } } }
        public BTCePair Currency { private get { return currency; } set { currency = value; Ticker.Stop(); Ticker.Start(); } }

        public FeeInfo DefaultFee { get { return FeeInfo.ReadFromJSON("{\"trade\":0.2}"); } }

        public static BTCeAPIWrapper Instance
        {
            get { 
                if (instance == null) 
                { 
                    lock (lockObject) 
                    { 
                        instance = new BTCeAPIWrapper(); 
                    } 
                } 
                return instance; 
            }
        }

        #endregion Properties

        #region Constructors

        static BTCeAPIWrapper()
        {
        }

        private BTCeAPIWrapper()
        {
            Ticker.Interval = TickerTimeout * 1000;
            Ticker.Elapsed += CheckForNewPrice;
            Ticker.Start();

            logger.Info("Ticker Timer started ...");

            Fee.Interval = FeeTimeout * 1000;
            Fee.Elapsed += CheckForNewFee;
            Fee.Start();

            ActiveOrders.Interval = ordersTimeout * 1000;
            ActiveOrders.Elapsed += GetActiveOrders;

            Info.Interval = infoTimeout * 1000;
            Info.Elapsed += GetAccountInfo;
        }

        #endregion Constructors

        #region Private Static Methods

        private static UInt32 GetNonce()
        {
            while (UnixTime.Now - lastNonce <= 1) ;
            lastNonce = UnixTime.Now;
            Nonce = lastNonce;

            return Nonce;
        }

        /*
        private static byte[] BuildPostData(Dictionary<string, string> d)
        {
            StringBuilder s = new StringBuilder();

            foreach (var item in d)
            {
                s.AppendFormat("{0}={1}", item.Key, HttpUtility .UrlEncode(item.Value));
                s.Append("&");
            }
            if (s.Length > 0)
            {
                s.Remove(s.Length - 1, 1);
            }

            return Encoding.ASCII.GetBytes(s.ToString());
        }
         * */

        private static string BuildPostData(Dictionary<string, string> d)
        {
            StringBuilder s = new StringBuilder();

            foreach (var item in d)
            {
                s.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode(item.Value));
                s.Append("&");
            }
            if (s.Length > 0)
            {
                s.Remove(s.Length - 1, 1);
            }

            return s.ToString();
        }

        private static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        #endregion Private Static Methods

        #region Private Methods

        private string Query(Dictionary<string, string> args)
        {
            var nonce = GetNonce().ToString();
            args.Add("nonce", nonce);
            var dataStr = BuildPostData(args);
            var data = Encoding.ASCII.GetBytes(dataStr);

            var headers = new WebHeaderCollection { { "Key", key }, { "Sign", ByteArrayToString(hashMaker.ComputeHash(data)).ToLower() } };

            var request = WebRequest.Create(new Uri(BTCeAPIPrivateURL)) as HttpWebRequest;
            if (request == null)
                throw new Exception("Non HTTP WebRequest");

            int TimeOut = 10;
            request.Headers = headers;
            request.Method = "POST";
            request.Timeout = TimeOut * 1000;
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            var reqStream = request.GetRequestStream();
            reqStream.Write(data, 0, data.Length);
            reqStream.Close();

            return (new StreamReader(request.GetResponse().GetResponseStream())).ReadToEnd();

        }

        private string DecimalToString(decimal d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }

        #endregion Private Methods

        #region Public Methods

        public void Credential(string key, string secret)
        {
            Info.Stop();
            ActiveOrders.Stop();

            this.key = key;
            hashMaker = new HMACSHA512(Encoding.ASCII.GetBytes(secret));

            startActiveOtrders = false;

            Info.Start();
        }

        public TradeAnswer PlaceOrder(BTCePair pair, BTCeTradeType type, decimal rate, decimal amount)
        {
            if (!authenticated)
            {
                throw new BTCeAPIException("Not Authenticated");
            }

            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "Trade" },
                { "pair", BtcePairHelper.ToString(pair) },
                { "type", TradeTypeHelper.ToString(type) },
                { "rate", DecimalToString(rate) },
                { "amount", DecimalToString(amount) }
            });

            return TradeAnswer.ReadFromJSON(resultStr);
        }

        #endregion Public Methods

        #region Timer Callbacks

        void CheckForNewFee(object sender, ElapsedEventArgs e)
        {
            Fee.Stop();

            if (FeeChanged != null)
            {
                new System.Threading.Thread(CallBTCeAPIFee).Start();

                return;
            }

            Fee.Start();
        }

        private void CheckForNewPrice(object sender, ElapsedEventArgs e)
        {
            Ticker.Stop();

            if (PriceChanged != null)
            {
                new System.Threading.Thread(CallBTCeAPITicker).Start();

                return;
            }

            Ticker.Start();
        }

        private void GetAccountInfo(object sender, ElapsedEventArgs e)
        {
            Info.Stop();

            if (InfoReceived != null)
            {
                new System.Threading.Thread(CallBTCeAPIAccountInfo).Start();

                return;
            }

            Info.Start();
        }

        void GetActiveOrders(object sender, ElapsedEventArgs e)
        {
            ActiveOrders.Stop();

            if (ActiveOrdersReceived != null)
            {
                new System.Threading.Thread(CallBTCeAPIActiveOrders).Start();

                return;
            }

            ActiveOrders.Start();
        }

        #endregion Timer Callbacks

        #region Thread Called Methods

        private void CallBTCeAPITicker()
        {
            int TimeOut = 10;
            string queryStr = string.Format(BTCeAPITickerURL, BtcePairHelper.ToString(currency));
            var request = (HttpWebRequest)WebRequest.Create(queryStr);
            request.GetResponse();
            request.Timeout = TimeOut * 1000;

            if (request == null)
            {
                throw new Exception("Non HTTP WebRequest");
            }

            if (PriceChanged != null)
            {
                PriceChanged(
                    TickerInfo.ReadFromJSON(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd()), 
                    EventArgs.Empty);
            }

            Ticker.Start();
        }

        private void CallBTCeAPIFee()
        {
            int TimeOut = 10;
            string queryStr = string.Format(BTCeAPIFeeURL, BtcePairHelper.ToString(currency));
            var request = (HttpWebRequest)WebRequest.Create(queryStr);
            request.GetResponse();
            request.Timeout = TimeOut * 1000;

            if (request == null)
            {
                throw new Exception("Non HTTP WebRequest");
            }

            if (FeeChanged != null)
            {
                FeeChanged(
                    FeeInfo.ReadFromJSON(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd()),
                    EventArgs.Empty);
            }

            Fee.Start();
        }

        private void CallBTCeAPIAccountInfo()
        {
            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "getInfo" }
            });

            if (InfoReceived != null)
            {
                try
                {
                    AccountInfo info = AccountInfo.ReadFromJSON(resultStr);
                    InfoReceived(info, EventArgs.Empty);

                    if (!startActiveOtrders)
                    {
                        ActiveOrders.Start();
                        startActiveOtrders = true;
                    }

                    Info.Start();
                }
                catch (BTCeAPIException e)
                {
                    InfoReceived(e, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    InfoReceived(ex, EventArgs.Empty);
                }

            }
        }

        private void CallBTCeAPIActiveOrders()
        {
            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "ActiveOrders" },
                { "pair", BtcePairHelper.ToString(currency) }
            });

            if (ActiveOrdersReceived != null)
            {
                try
                {
                    OrdersList orders = OrdersList.ReadFromJSON(resultStr);
                    ActiveOrdersReceived(orders, EventArgs.Empty);

                    ActiveOrders.Start();
                }
                catch (BTCeAPIException e)
                {
                    ActiveOrdersReceived(e, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    ActiveOrdersReceived(ex, EventArgs.Empty);
                }

            }
        }

        #endregion Thread Called Methods
    }
}
