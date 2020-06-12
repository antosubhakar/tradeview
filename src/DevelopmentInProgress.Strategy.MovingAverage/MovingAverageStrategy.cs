﻿using DevelopmentInProgress.Strategy.Common;
using DevelopmentInProgress.Strategy.Common.Parameter;
using DevelopmentInProgress.Strategy.Common.StrategyTrade;
using DevelopmentInProgress.Strategy.Common.TradeCreator;
using DevelopmentInProgress.TradeView.Core.Enums;
using DevelopmentInProgress.TradeView.Core.Events;
using DevelopmentInProgress.TradeView.Core.Extensions;
using DevelopmentInProgress.TradeView.Core.Interfaces;
using DevelopmentInProgress.TradeView.Core.Model;
using DevelopmentInProgress.TradeView.Core.Strategy;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevelopmentInProgress.Strategy.MovingAverage
{
    public class MovingAverageStrategy : TradeStrategyBase
    {
        private MovingAverageTradeParameters movingAverageTradeParameters;
        private TradeCache<MovingAverageTradeCreator, MovingAverageTrade, MovingAverageTradeParameters> tradeCache;

        public override async Task<bool> TryUpdateStrategyAsync(string parameters)
        {
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                var strategyParameters = JsonConvert.DeserializeObject<MovingAverageTradeParameters>(parameters);

                if(tradeCache == null)
                {
                    tradeCache = new TradeCache<MovingAverageTradeCreator, MovingAverageTrade, MovingAverageTradeParameters>(strategyParameters.TradeRange);
                }

                if(movingAverageTradeParameters == null 
                   || !strategyParameters.MovingAvarageRange.Equals(movingAverageTradeParameters.MovingAvarageRange)
                   || !strategyParameters.SellIndicator.Equals(movingAverageTradeParameters.SellIndicator)
                   || !strategyParameters.BuyIndicator.Equals(movingAverageTradeParameters.BuyIndicator))
                {
                    movingAverageTradeParameters = strategyParameters;
                    Strategy.Parameters = parameters;
                    tradeCache.TradeCreator.Reset(movingAverageTradeParameters);
                }

                Suspend = movingAverageTradeParameters.Suspend;

                StrategyParameterUpdateNotification(new StrategyNotificationEventArgs { StrategyNotification = new StrategyNotification { Name = Strategy.Name, Message = parameters, NotificationLevel = NotificationLevel.ParameterUpdate } });

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return await tcs.Task;
        }

        public override void SubscribeTrades(TradeEventArgs tradeEventArgs)
        {
            if (Strategy == null)
            {
                return;
            }

            var strategyNotification = new StrategyNotification { Name = Strategy.Name, NotificationLevel = NotificationLevel.Trade };
            string message;

            try
            {
                var previousLastTrade = tradeCache.GetLastTrade();

                ITrade[] trades;
                MovingAverageTrade[] movingAverageTrades;

                if (previousLastTrade == null)
                {
                    trades = (from t in tradeEventArgs.Trades
                                  orderby t.Time, t.Id
                                  select t).ToArray();
                    movingAverageTrades = tradeCache.AddRange(trades);
                }
                else
                {
                    trades = (from t in tradeEventArgs.Trades
                                  where t.Time > previousLastTrade.Time && t.Id > previousLastTrade.Id
                                  orderby t.Time, t.Id
                                  select t).ToArray();
                    movingAverageTrades = tradeCache.AddRange(trades);
                }

                var lastTrade = tradeCache.GetLastTrade();

                if (!Suspend)
                {
                    PlaceOrder(lastTrade);
                }

                message = JsonConvert.SerializeObject(movingAverageTrades);
            }
            catch (Exception ex)
            {
                message = JsonConvert.SerializeObject(ex);
            }

            strategyNotification.Message = message;
            StrategyTradeNotification(new StrategyNotificationEventArgs { StrategyNotification = strategyNotification });
        }

        private void PlaceOrder(MovingAverageTrade trade)
        {
            if (AccountInfo == null
                || PlacingOrder)
            {
                return;
            }

            lock (AccountLock)
            {
                try
                {                    
                    var symbol = ExchangeSymbols[Exchange.Binance].Single(s => s.ExchangeSymbol.Equals(trade.Symbol));

                    OrderSide orderSide;
                    decimal stopPrice = 0m;

                    if (trade.Price > trade.SellPrice)
                    {
                        orderSide = OrderSide.Sell;
                        stopPrice = trade.SellPrice.HasRemainder() ? trade.SellPrice.Trim(symbol.Price.Increment.GetPrecision()) : trade.SellPrice;
                    }
                    else if (trade.Price < trade.BuyPrice)
                    {
                        orderSide = OrderSide.Buy;
                        stopPrice = trade.BuyPrice.HasRemainder() ? trade.BuyPrice.Trim(symbol.Price.Increment.GetPrecision()) : trade.BuyPrice;
                    }
                    else
                    {
                        return;
                    }

                    decimal quantity = 0m;

                    var quoteAssetBalance = AccountInfo.Balances.FirstOrDefault(b => b.Asset.Equals(symbol.QuoteAsset.Symbol));
                    var baseAssetBalance = AccountInfo.Balances.FirstOrDefault(b => b.Asset.Equals(symbol.BaseAsset.Symbol));

                    if (orderSide.Equals(OrderSide.Buy)
                        && quoteAssetBalance.Free > 0)
                    {
                        quantity = quoteAssetBalance.Free / trade.Price;
                    }
                    else if (orderSide.Equals(OrderSide.Sell)
                        && baseAssetBalance.Free > 0)
                    {
                        quantity = baseAssetBalance.Free;
                    }

                    var trimmedQuantity = quantity.HasRemainder() ? quantity.Trim(symbol.Quantity.Increment.GetPrecision()) : quantity;
                    var trimmedPrice = trade.Price.HasRemainder() ? trade.Price.Trim(symbol.Price.Increment.GetPrecision()) : trade.Price;

                    var value = trimmedQuantity * trimmedPrice;

                    if (value > symbol.NotionalMinimumValue)
                    {
                        var clientOrder = new ClientOrder
                        {
                            Symbol = trade.Symbol,
                            Price = trimmedPrice,
                            StopPrice = stopPrice,
                            Type = OrderType.Limit,
                            Side = orderSide,
                            Quantity = trimmedQuantity,
                            QuoteAccountBalance = quoteAssetBalance,
                            BaseAccountBalance = baseAssetBalance
                        };

                        symbol.ValidateClientOrder(clientOrder);

                        var strategySymbol = Strategy.StrategySubscriptions.SingleOrDefault(ss => ss.Symbol.Equals(trade.Symbol));
                        var user = new User { ApiKey = strategySymbol.ApiKey, ApiSecret = strategySymbol.SecretKey, Exchange = Exchange.Binance };
                        var order = ExchangeServices[Exchange.Binance].PlaceOrder(user.Exchange, user, clientOrder).Result;

                        var message = $"{clientOrder.Symbol} {order.Price} {clientOrder.Quantity} {clientOrder.Side} {clientOrder.Type}";

                        StrategyNotification(new StrategyNotificationEventArgs { StrategyNotification = new StrategyNotification { Name = Strategy.Name, Message = message, NotificationLevel = NotificationLevel.Information } });
                    }
                    else
                    {
                        PlacingOrder = false;
                    }
                }
                catch (Exception ex)
                {
                    PlacingOrder = false;
                    StrategyNotification(new StrategyNotificationEventArgs { StrategyNotification = new StrategyNotification { Name = Strategy.Name, Message = ex.Message, NotificationLevel = NotificationLevel.Information } });
                }
            }
        }
    }
}