﻿using DevelopmentInProgress.MarketView.Interface.Strategy;

namespace DevelopmentInProgress.Wpf.Common.Model
{
    public class StrategySubscription : EntityBase
    {
        public string Symbol { get; set; }
        public int Limit { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public Exchange Exchange { get; set; }
        public bool SubscribeAccount { get; set; }
        public bool SubscribeOrderBook { get; set; }
        public bool SubscribeTrades { get; set; }
        public bool SubscribeStatistics { get; set; }
        public string SelectedExchange { get; set; }
    }
}