﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using WampSharp.Binding;
using WampSharp.V2;
using WampSharp.V2.Rpc;

namespace PoloniexClient {
    public class PoloniexModel {
        const string PoloniexServerAddress = "wss://api.poloniex.com";

        static PoloniexModel defaultModel;
        public static PoloniexModel Default {
            get {
                if(defaultModel == null)
                    defaultModel = new PoloniexModel();
                return defaultModel;
            }
        }

        List<PoloniexTicker> tickers;
        public List<PoloniexTicker> Tickers {
            get {
                if(tickers == null)
                    tickers = new List<PoloniexTicker>();
                return tickers;
            }
        }

        protected IDisposable TickersSubscriber { get; set; }
        public void Connect() {
            if(TickersSubscriber != null)
                TickersSubscriber.Dispose();
            TickersSubscriber = null;
            TickersSubscriber = SubscribeToTicker();
        }

        private IDisposable SubscribeToTicker() {
            DefaultWampChannelFactory channelFactory =
                new DefaultWampChannelFactory();

            IWampChannel wampChannel = channelFactory.CreateJsonChannel(PoloniexServerAddress, "realm1");
            wampChannel.Open().Wait();

            ISubject<PoloniexTicker> subject = wampChannel.RealmProxy.Services.GetSubject<PoloniexTicker>("ticker", new PoloniexTickerConverter());
            IDisposable disposable = subject.Subscribe(x => GetTickerItem(x));

            return disposable;
        }


        private void GetTickerItem(PoloniexTicker item) {
            lock(Tickers) {
                PoloniexTicker t = Tickers.FirstOrDefault((i) => i.CurrencyPair == item.CurrencyPair);
                if(t != null) {
                    lock(t) {
                        t.Assign(item);
                        t.UpdateHistoryItem();
                        RaiseTickerUpdate(t);
                    }
                }
                else {
                    Tickers.Add(item);
                    RaiseTickerUpdate(item);
                }
            }
        }

        public event TickerUpdateEventHandler TickerUpdate;
        protected void RaiseTickerUpdate(PoloniexTicker t) {
            TickerUpdateEventArgs e = new TickerUpdateEventArgs() { Ticker = t };
            if(TickerUpdate != null)
                TickerUpdate(this, e);
            t.RaiseChanged();
        }
        public IDisposable ConnectOrderBook(PoloniexOrderBook orderBook) {
            orderBook.Updates.Clear();
            DefaultWampChannelFactory channelFactory =
               new DefaultWampChannelFactory();

            IWampChannel wampChannel = channelFactory.CreateJsonChannel(PoloniexServerAddress, "realm1");
            wampChannel.Open().Wait();

            ISubject<PoloniexOrderBookUpdateInfo> subject = wampChannel.RealmProxy.Services.GetSubject<PoloniexOrderBookUpdateInfo>(orderBook.Owner.CurrencyPair, new OrderBookUpdateInfoConverter());
            return subject.Subscribe(x => orderBook.OnRecvUpdate(x));
        }
        public void GetTickerSnapshot() {
            string address = "https://poloniex.com/public?command=returnTicker";
            string text;
            using(WebClient client = new WebClient()) {
                text = client.DownloadString(address);
            }
            Tickers.Clear();
            JObject res = (JObject)JsonConvert.DeserializeObject(text);
            foreach(JProperty prop in res.Children()) {
                PoloniexTicker t = new PoloniexTicker();
                t.CurrencyPair = prop.Name;
                JObject obj = (JObject)prop.Value;
                t.Id = obj.Value<int>("id");
                t.Last = obj.Value<double>("last");
                t.LowestAsk = obj.Value<double>("lowestAsk");
                t.HighestBid = obj.Value<double>("highestBid");
                t.PercentChange = obj.Value<double>("percentChange");
                t.BaseVolume = obj.Value<double>("baseVolume");
                t.QuoteVolume = obj.Value<double>("quoteVolume");
                t.IsFrozen = obj.Value<int>("isFrozen") != 0;
                t.Hr24High = obj.Value<double>("high24hr");
                t.Hr24Low = obj.Value<double>("low24hr");
                Tickers.Add(t);
            }
        }
        public void GetSnapshot(PoloniexOrderBook poloniexOrderBook) {
            string address = string.Format("https://poloniex.com/public?command=returnOrderBook&currencyPair={0}&depth=10000",
                Uri.EscapeDataString(poloniexOrderBook.Owner.CurrencyPair));
            string text;
            using(WebClient client = new WebClient()) {
                text = client.DownloadString(address);
            }
            poloniexOrderBook.Bids.Clear();
            poloniexOrderBook.Asks.Clear();

            JObject res = (JObject)JsonConvert.DeserializeObject(text);
            foreach(JProperty prop in res.Children()) {
                if(prop.Name == "asks" || prop.Name == "bids") {
                    OrderBookEntryType type = prop.Name == "asks" ? OrderBookEntryType.Ask : OrderBookEntryType.Bid;
                    JArray items = prop.Value.ToObject<JArray>();
                    foreach(JArray item in items.Children()) {
                        PoloniexOrderBookUpdateInfo info = new PoloniexOrderBookUpdateInfo();
                        info.Type = prop.Name == "asks" ? OrderBookEntryType.Ask : OrderBookEntryType.Bid;
                        info.Entry = new PoloniexOrderBookEntry();
                        info.Update = OrderBookUpdateType.Modify;
                        JEnumerable<JToken> values = (JEnumerable<JToken>)item.Children();
                        JValue rateValue = (JValue)values.First();
                        info.Entry.Rate = rateValue.ToObject<double>();
                        info.Entry.Amount = rateValue.Next.ToObject<double>();
                        if(type == OrderBookEntryType.Ask)
                            poloniexOrderBook.ForceAddAsk(info);
                        else
                            poloniexOrderBook.ForceAddBid(info);
                    }
                }
                else if(prop.Name == "seq") {
                    poloniexOrderBook.SeqNumber = prop.Value.ToObject<int>();
                    Console.WriteLine("Snapshot seq no = " + poloniexOrderBook.SeqNumber);
                }
                else if(prop.Name == "isFrozen") {
                    poloniexOrderBook.Owner.IsFrozen = prop.Value.ToObject<int>() == 0;
                }
            }
            poloniexOrderBook.ApplyQueueUpdates();
        }
    }

    public delegate void TickerUpdateEventHandler(object sender, TickerUpdateEventArgs e);
    public class TickerUpdateEventArgs : EventArgs {
        public PoloniexTicker Ticker { get; set; }
    }
 }
