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
    #region Helper classes

    /// <summary>
    /// Helper class for converting from UnixTime to .NET DateTime object
    /// </summary>
    static class UnixTime
    {
        /// <summary>
        /// Initial Reference Date for Unix Time conversions
        /// </summary>
        static DateTime unixEpoch;

        #region Constructors

        /// <summary>
        /// Default Constrcutor - initialize referent Date - 1970-01-01
        /// </summary>
        static UnixTime()
        {
            unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        #endregion Constructors

        /// <summary>
        /// Returns current Unix Time
        /// </summary>
        public static UInt32 Now { get { return GetFromDateTime(DateTime.UtcNow); } }

        /// <summary>
        /// Returns Unix Time for passed Date
        /// </summary>
        /// <param name="d">Referent Date</param>
        /// <returns>Total seconds</returns>
        public static UInt32 GetFromDateTime(DateTime d) { return (UInt32)(d - unixEpoch).TotalSeconds; }

        /// <summary>
        /// Converts Unix Time to .NET DateTime object
        /// </summary>
        /// <param name="unixtime">Unix Time to convert</param>
        /// <returns>.NET DateTime object</returns>
        public static DateTime ConvertToDateTime(UInt32 unixtime) { return unixEpoch.AddSeconds(unixtime); }
    }

    #endregion Helper classes

    /// <summary>
    /// BTCe API Wrapper
    /// </summary>
    public class BTCeAPIWrapper
    {
        #region Constanrs

        /// <summary>
        /// Indicates Price Change event to be pushed always
        /// </summary>
        public const int PUSH_PRICE_CHANGE_ALWAYS = 0;

        /// <summary>
        /// Indicates Price Change event to be pushed when BUY price change
        /// </summary>
        public const int PUSH_PRICE_CHANGE_BUY = 1;

        /// <summary>
        /// Indicates Price Change event to be pushed when SELL price change
        /// </summary>
        public const int PUSH_PRICE_CHANGE_SELL = 2;

        /// <summary>
        /// Indicates Price Change event to be pushed when BUY price is greater then privious one
        /// </summary>
        public const int PUSH_PRICE_CHANGE_BUY_UP = 4;

        /// <summary>
        /// Indicates Price Change event to be pushed when BUY price is lower then privious one
        /// </summary>
        public const int PUSH_PRICE_CHANGE_BUY_DOWN = 8;

        /// <summary>
        /// Indicates Price Change event to be pushed when SELL price is greater then privious one
        /// </summary>
        public const int PUSH_PRICE_CHANGE_SELL_UP = 16;

        /// <summary>
        /// Indicates Price Change event to be pushed when SELL price is lower then privious one
        /// </summary>
        public const int PUSH_PRICE_CHANGE_SELL_DOWN = 32;

        public const int PRICE_CHANGED_BUY_UP = 1;
        public const int PRICE_CHANGED_BUY_DOWN = 2;
        public const int PRICE_CHANGED_SELL_UP = 4;
        public const int PRICE_CHANGED_SELL_DOWN = 8;

        public const int INFO_FIRST_TIME_CALL = 1;
        public const int INFO_OPEN_ORDERS_CHANGED = 2;
        public const int INFO_CURRENCY_AMOUNT_CHANGED = 4;
        public const int INFO_RIGHTS_CHANGED = 8;
        public const int INFO_EXCEPTION = 16;

        #endregion Constanrs

        #region Static Members

        private static ILog logger = log4net.LogManager.GetLogger("Console");

        private static BTCeAPIWrapper instance;
        private static object lockObject = new object();

        private static string BTCeAPITickerURL = "https://btc-e.com/api/2/{0}/ticker";
        private static string BTCeAPIFeeURL = "https://btc-e.com/api/2/{0}/fee";
        private static string BTCeAPIPrivateURL = "https://btc-e.com/tapi";
        private static UInt32 Nonce = UnixTime.Now;
        private static UInt32 lastNonce = 0;

        #endregion Static Members

        #region Private members

        private Timer Ticker = new Timer();
        private Timer Fee = new Timer();
        private Timer Info = new Timer();

        private int tickerTimeout = 1;
        private int feeTimeout = 1;
        private int infoTimeout = 1;
        private BTCePair currency = BTCePair.btc_usd;
        private int WebCallTimeOut = 10;

        private string key;
        private HMACSHA512 hashMaker;

        private bool authenticated = false;

        private TickerInfo latestTicker = null;
        private FeeInfo latestFee = null;
        private AccountInfo latestAccountInfo = null;

        private object lockCredentials = new object();
        
        #endregion Private members

        #region Public Event Handlers

        /// <summary>
        /// Public API Event
        /// Event Handler fired when new Price information is retrieved
        /// </summary>
        public EventHandler PriceChanged;

        /// <summary>
        /// Public API Event
        /// Event Handler fired when new Fee information is received
        /// </summary>
        public EventHandler FeeChanged;

        /// <summary>
        /// Account Info related Event
        /// Event Handler fired when some Account Information is changed or retrieved for the first time
        /// Additional information about changed part will be send as part f the Event data
        /// Available indicators for changed data:
        ///     FIRST_TIME_CALL - Account Information is received for the first time
        ///     OPEN_ORDERS_CHANGED - Open Orders count changed
        ///     CURRENCY_AMOUNT_CHANGED - any change in the available Currencies
        ///         As part of additional Event infromation List with Currencies will be send
        ///     RIGHTS_CHANGED - any change in Account Rights
        ///     EXCEPTION - teher is exception thrown during getting Account Information process
        /// This event should be used after Authentication data is provided
        ///     see method Credential(string key, string secret)
        /// </summary>
        public EventHandler InfoChanged;

        #endregion Public Event Handlers

        #region Properties

        /// <summary>
        /// Sets Ticker/Price change retrieval period in seconds
        /// Default period is 1 second
        /// </summary>
        public int TickerTimeout { private get { return tickerTimeout; } set { Ticker.Stop(); tickerTimeout = value; Ticker.Interval = tickerTimeout * 1000; Ticker.Start(); } }

        /// <summary>
        /// Sets Fee change retrieval period in seconds
        /// Default period is 259200 seconds (3 hours)
        /// </summary>
        public int FeeTimeout { private get { return feeTimeout; } set { Fee.Stop(); feeTimeout = value; Fee.Interval = feeTimeout * 1000; Fee.Start(); } }

        /// <summary>
        /// Sets Active Orders change retrieval period in seconds
        /// Default period is 5 seconds
        /// </summary>
        // public int OrdersTimeout { private get { return ordersTimeout; } set { ActiveOrders.Stop(); ordersTimeout = value; ActiveOrders.Interval = ordersTimeout * 1000; if (startActiveOtrders) { ActiveOrders.Start(); } } }

        /// <summary>
        /// Sets active Pair (Currency) that will be used for Ticker/Price and Active Orders retrieval
        /// DEfault value is btc_usd
        /// </summary>
        public BTCePair Currency { private get { return currency; } set { currency = value; Ticker.Stop(); Ticker.Start(); } }

        /// <summary>
        /// Gets Default Fee as default Fee rerieval period is every 3 hours and before next retrieval
        /// Also this Default Fee can be used if there is API issues with retrieving onine Fee
        /// </summary>
        public FeeInfo DefaultFee { get { return FeeInfo.ReadFromJSON("{\"trade\":0.2}"); } }

        /// <summary>
        /// Indicates which Price Change should be monitored in order to push Price information
        /// Use the following available constants (you can combine then using |):
        ///     PUSH_PRICE_CHANGE_ALWAYS - always send latest Price information (default value)
        ///     PUSH_PRICE_CHANGE_BUY - send Price infromation when there is change in BUY price
        ///     PUSH_PRICE_CHANGE_SELL - send Price infromation when there is change in SELL price
        ///     PUSH_PRICE_CHANGE_BUY_UP - send Price infromation when there is increase in BUY price
        ///     PUSH_PRICE_CHANGE_BUY_DOWN - send Price infromation when there is decrease in BUY price
        ///     PUSH_PRICE_CHANGE_SELL_UP - send Price infromation when there is increase in SELL price
        ///     PUSH_PRICE_CHANGE_SELL_DOWN - send Price infromation when there is decrease in SELL price
        /// </summary>
        public int PriceChangePushIndicator { private get; set; }

        /// <summary>
        /// Returns BTCeAPIWrapper object
        /// </summary>
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
            PriceChangePushIndicator = PUSH_PRICE_CHANGE_ALWAYS;

            Ticker.Interval = TickerTimeout * 1000;
            Ticker.Elapsed += CheckForNewPrice;
            Ticker.Start();

            logger.Info("Ticker Timer started ...");

            Fee.Interval = FeeTimeout * 1000;
            Fee.Elapsed += CheckForNewFee;
            Fee.Start();
            CheckForNewFee(null, null);

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

            request.Headers = headers;
            request.Method = "POST";
            request.Timeout = WebCallTimeOut * 1000;
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

        /// <summary>
        /// Sets Credetials that will be used for Authenticating against BTCe services
        /// </summary>
        /// <param name="key">BTCe API Key</param>
        /// <param name="secret">BTCe API Sercet - it will be used once to create HMAC-SHA512 object for signing API requests </param>
        /// <exception cref="BTCeAPI.BTCeAPIWrapper.BTCeAPIException">Thrown when privided credentials are not accepted by BTCe API</exception>
        public void Credential(string key, string secret)
        {
            Info.Stop();

            authenticated = false;

            lock (lockCredentials)
            {
                this.key = key;
                hashMaker = new HMACSHA512(Encoding.ASCII.GetBytes(secret));
            }

            try
            {
                var resultStr = Query(new Dictionary<string, string>()
                {
                    { "method", "getInfo" }
                });

                AccountInfo info = AccountInfo.ReadFromJSON(resultStr);

                authenticated = true;
                
                Info.Start();
            }
            catch (Exception)
            {
                throw new BTCeAPIException("Invalid credentials");
            }
        }

        /// <summary>
        /// Place single trade with passed parameters
        /// </summary>
        /// <param name="pair">Trade Pair (currency)</param>
        /// <param name="type">Trade Type</param>
        /// <param name="rate">Trade Rate</param>
        /// <param name="amount">Trade Amount</param>
        /// <returns>On successful operation will return populated TradeAnswer, on error will throw BTCeException with original BTCe API error message</returns>
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

        /// <summary>
        /// Retrieves Order infromation for provided Order ID.
        /// If no Order with provided ID is found - BTCeException is thrown
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <returns>Order information for supplied ID</returns>
        public OrderInfo GetOrderInformation(int orderId)
        {
            if (!authenticated)
            {
                throw new BTCeAPIException("Not Authenticated");
            }

            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "OrderInfo" },
                { "order_id", orderId.ToString() }
            });

            return OrderInfo.ReadFromJSON(resultStr);
        }

        /// <summary>
        /// Retrieves Active Orders infromation.
        /// </summary>
        /// <returns>List with Active Orders</returns>
        public OrdersList GetActiveOrders()
        {
            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "ActiveOrders" },
                { "pair", BtcePairHelper.ToString(currency) }
            });

            try
            {
                OrdersList orders = OrdersList.ReadFromJSON(resultStr);

                return orders;
            }
            catch (BTCeAPIException e)
            {
                logger.Error("BTC Error while retrieving Active Orders", e);
                throw e;
            }
            catch (Exception ex)
            {
                logger.Error("Error while retrieving Active Orders", ex);
                throw ex;
            }
        }

        #endregion Public Methods

        #region Timer Callbacks

        private void CheckForNewFee(object sender, ElapsedEventArgs e)
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

            if (InfoChanged != null)
            {
                new System.Threading.Thread(CallBTCeAPIAccountInfo).Start();

                return;
            }

            Info.Start();
        }

        #endregion Timer Callbacks

        #region Thread Called Methods

        private void CallBTCeAPITicker()
        {
            string queryStr = string.Format(BTCeAPITickerURL, BtcePairHelper.ToString(currency));
            var request = (HttpWebRequest)WebRequest.Create(queryStr);
            request.GetResponse();
            request.Timeout = WebCallTimeOut * 1000;

            if (request == null)
            {
                throw new Exception("Non HTTP WebRequest");
            }

            TickerInfo ticker = TickerInfo.ReadFromJSON(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

            if (PriceChanged != null)
            {
                if (
                    latestTicker == null ||
                    PriceChangePushIndicator == PUSH_PRICE_CHANGE_ALWAYS ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_BUY) > 0 && latestTicker.Buy != ticker.Buy) ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_SELL) > 0 && latestTicker.Sell != ticker.Sell) ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_BUY_DOWN) > 0 && latestTicker.Buy > ticker.Buy) ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_BUY_UP) > 0 && latestTicker.Buy < ticker.Buy) ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_SELL_DOWN) > 0 && latestTicker.Sell > ticker.Sell) ||
                    ((PriceChangePushIndicator & PUSH_PRICE_CHANGE_SELL_UP) > 0 && latestTicker.Sell < ticker.Sell)
                )
                {
                    PriceChangedEventArgs eventArgs = new PriceChangedEventArgs(0);

                    if (latestTicker != null)
                    {
                        int changedIndicator = 0;

                        if (latestTicker.Buy < ticker.Buy)
                        {
                            changedIndicator |= PRICE_CHANGED_BUY_UP;
                        }

                        if (latestTicker.Buy > ticker.Buy)
                        {
                            changedIndicator |= PRICE_CHANGED_BUY_DOWN;
                        }

                        if (latestTicker.Sell < ticker.Sell)
                        {
                            changedIndicator |= PRICE_CHANGED_SELL_UP;
                        }

                        if (latestTicker.Sell > ticker.Sell)
                        {
                            changedIndicator |= PRICE_CHANGED_SELL_DOWN;
                        }

                        eventArgs = new PriceChangedEventArgs(changedIndicator);
                    }

                    PriceChanged(ticker, eventArgs);
                }
            }

            latestTicker = ticker;

            Ticker.Start();
        }

        private void CallBTCeAPIFee()
        {
            string queryStr = string.Format(BTCeAPIFeeURL, BtcePairHelper.ToString(currency));
            var request = (HttpWebRequest)WebRequest.Create(queryStr);
            request.GetResponse();
            request.Timeout = WebCallTimeOut * 1000;

            if (request == null)
            {
                throw new Exception("Non HTTP WebRequest");
            }

            FeeInfo currentFee = FeeInfo.ReadFromJSON(new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd());

            if (FeeChanged != null)
            {
                if (latestFee == null ||
                    latestFee.Fee != currentFee.Fee)
                {
                    FeeChanged(currentFee, EventArgs.Empty);
                }
            }

            latestFee = currentFee;

            Fee.Start();
        }

        private void CallBTCeAPIAccountInfo()
        {
            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "getInfo" }
            });

            try
            {
                AccountInfo info = AccountInfo.ReadFromJSON(resultStr);

                if (latestAccountInfo == null)
                {
                    latestAccountInfo = info;

                    if (InfoChanged != null)
                    {
                        InfoChanged(latestAccountInfo, new AccoutnInformationEventArgs(INFO_FIRST_TIME_CALL, null));
                    }

                    Info.Start();

                    return;
                }

                int infoChangeReason = 0;

                if (latestAccountInfo.Rights.Info != info.Rights.Info ||
                    latestAccountInfo.Rights.Trade != info.Rights.Trade)
                {
                    infoChangeReason |= INFO_RIGHTS_CHANGED;
                }

                if (latestAccountInfo.OpenOrders != info.OpenOrders)
                {
                    infoChangeReason |= INFO_OPEN_ORDERS_CHANGED;
                }

                List<BTCeCurrency> changedCurrencies = new List<BTCeCurrency>();
                foreach (Currency currency in latestAccountInfo.Currencies)
                {
                    Currency c = info.Currencies.Find(x => x.Name == currency.Name);
                    if (c != null &&
                        c.Value != currency.Value)
                    {
                        infoChangeReason |= INFO_CURRENCY_AMOUNT_CHANGED;
                        changedCurrencies.Add(currency.Name);
                    }
                }

                if (infoChangeReason != 0)
                {
                    if (InfoChanged != null)
                    {
                        if (changedCurrencies.Count > 0)
                        {
                            InfoChanged(latestAccountInfo, new AccoutnInformationEventArgs(infoChangeReason, changedCurrencies));
                        }
                        else
                        {
                            InfoChanged(latestAccountInfo, new AccoutnInformationEventArgs(infoChangeReason, null));
                        }
                    }
                }

                latestAccountInfo = info;

                Info.Start();
            }
            catch (BTCeAPIException e)
            {
                authenticated = false;
            }
            catch (Exception ex)
            {
                Info.Start();
            }
        }

        #endregion Thread Called Methods
    }
}
