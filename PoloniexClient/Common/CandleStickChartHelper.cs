﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoMarketClient {
    public class CandleStickData : INotifyPropertyChanged {
        DateTime time;
        public DateTime Time {
            get { return time; }
            set {
                if(Time == value)
                    return;
                time = value;
                RaisePropertyChanged("Time");
            }
        }
        decimal open;
        public decimal Open {
            get { return open; }
            set {
                if(Open == value)
                    return;
                open = value;
                RaisePropertyChanged("Open");
            }
        }

        decimal close;
        public decimal Close {
            get { return close; }
            set {
                if(Close == value)
                    return;
                close = value;
                RaisePropertyChanged("Close");
            }
        }

        decimal high;
        public decimal High {
            get { return high; }
            set {
                if(High == value)
                    return;
                high = value;
                RaisePropertyChanged("High");
            }
        }

        decimal low;
        public decimal Low {
            get { return low; }
            set {
                if(Low == value)
                    return;
                low = value;
                RaisePropertyChanged("Low");
            }
        }

        decimal volume;
        public decimal Volume {
            get { return volume; }
            set {
                if(Volume == value)
                    return;
                volume = value;
                RaisePropertyChanged("Volume");
            }
        }

        decimal quoteVolume;
        public decimal QuoteVolume {
            get { return quoteVolume; }
            set {
                if(QuoteVolume == value)
                    return;
                quoteVolume = value;
                RaisePropertyChanged("QuoteVolume");
            }
        }

        decimal weightedAverage;
        public decimal WeightedAverage {
            get { return volume; }
            set {
                if(WeightedAverage == value)
                    return;
                weightedAverage = value;
                RaisePropertyChanged("WeightedAverage");
            }
        }

        event PropertyChangedEventHandler propertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add { this.propertyChanged += value; }
            remove { this.propertyChanged -= value; }
        }

        protected void RaisePropertyChanged(string propName) {
            if(this.propertyChanged != null)
                this.propertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public static class CandleStickChartHelper {
        public static BindingList<CandleStickData> CreateCandleStickData(IList<TickerHistoryItem> list, long rangeInSeconds) {
            BindingList<CandleStickData> res = new BindingList<CandleStickData>();
            CandleStickData candleItem = null;
            long maxTickCount = rangeInSeconds * TimeSpan.TicksPerSecond;
            foreach(TickerHistoryItem item in list) {
                if(candleItem == null || (item.Time.Ticks - candleItem.Time.Ticks > maxTickCount)) {
                    candleItem = new CandleStickData();
                    candleItem.Time = item.Time;
                    candleItem.Open = item.Current;
                    candleItem.Low = candleItem.High = candleItem.Close = item.Current;
                    res.Add(candleItem);
                    continue;
                }
                candleItem.Close = item.Current;
                candleItem.Low = Math.Min(candleItem.Low, item.Current);
                candleItem.High = Math.Max(candleItem.High, item.Current);
            }
            return res;
        }
        public static void AddCandleStickData(IList<CandleStickData> list, TickerHistoryItem item, long rangeInSeconds) {
            CandleStickData candleItem = null;
            long maxTickCount = rangeInSeconds * TimeSpan.TicksPerSecond;
            if(list.Count == 0 || (item.Time.Ticks - list[list.Count - 1].Time.Ticks) > maxTickCount) {
                candleItem = new CandleStickData();
                candleItem.Open = candleItem.Close = candleItem.High = candleItem.Low = item.Current;
                candleItem.Time = item.Time;
                list.Add(candleItem);
                return;
            }
            candleItem = list[list.Count - 1];
            candleItem.Close = item.Current;
            candleItem.Low = Math.Min(candleItem.Low, item.Current);
            candleItem.High = Math.Max(candleItem.High, item.Current);
            return;
        }
        public static IList<CandleStickData> CreateCandleStickData(TickerBase ticker) {
            ticker.CandleStickData.Clear();
            return CreateCandleStickData(ticker.History, ticker.CandleStickPeriodMin * 60);
        }
    }
}