﻿using DevelopmentInProgress.MarketView.Interface.Interfaces;
using DevelopmentInProgress.Wpf.Common.Extensions;
using DevelopmentInProgress.Wpf.Common.Model;
using LiveCharts;
using Prism.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DevelopmentInProgress.Wpf.Common.Helpers
{
    public static class TradeHelper
    {
        public static void UpdateTrades(
            IEnumerable<ITrade> tradesUpdate,
            int pricePrecision, 
            int quantityPrecision, 
            int tradesDisplayCount, 
            int tradesChartDisplayCount, 
            ILoggerFacade logger,
            ref List<TradeBase> trades,
            ref ChartValues<TradeBase> tradesChart)
        {
            var sw = new Stopwatch();
            sw.Start();
            logger.Log($"Start TradeUpdate", Category.Info, Priority.Low);

            if (trades == null)
            {
                // First set of incoming trades

                // Order by oldest to newest (as it will appear in chart).
                var newTrades = (from t in tradesUpdate
                                 orderby t.Time, t.Id
                                 select new TradeBase
                                 {
                                     Id = t.Id,
                                     Time = t.Time,
                                     Price = t.Price.Trim(pricePrecision),
                                     Quantity = t.Quantity.Trim(quantityPrecision),
                                     IsBuyerMaker = t.IsBuyerMaker
                                 }).ToList();

                var newTradesCount = newTrades.Count;

                if (newTradesCount > tradesChartDisplayCount)
                {
                    // More new trades than the chart can take, only takes the newest trades.
                    var chartTrades = newTrades.Skip(newTradesCount - tradesChartDisplayCount).ToList();
                    tradesChart = new ChartValues<TradeBase>(chartTrades);
                }
                else
                {
                    // New trades less (or equal) the 
                    // total trades to show in the chart.
                    tradesChart = new ChartValues<TradeBase>(newTrades);
                }

                if (newTradesCount > tradesDisplayCount)
                {
                    // More new trades than the list can take, only takes the newest trades.
                    var tradeBooktrades = newTrades.Skip(newTradesCount - tradesDisplayCount).ToList();

                    // Order by newest to oldest (as it will appear on trade list)
                    trades = new List<TradeBase>(tradeBooktrades.Reverse<TradeBase>().ToList());
                }
                else
                {
                    // New trades less (or equal) the 
                    // total trades to show in the trade list.
                    // Order by newest to oldest (as it will appear on trade list)
                    trades = new List<TradeBase>(newTrades.Reverse<TradeBase>().ToList());
                }
            }
            else
            {
                // Subsequent set of new trades

                // Get the latest available trade - the first trade on the 
                // trade list (which is also the last trade in the chart).
                var first = trades.First();

                // Extract new trades where time and id is greater than latest available trade. 
                // Order by oldest to newest (as it will appear in chart).
                var newTrades = (from t in tradesUpdate
                                 where t.Time > first.Time && t.Id > first.Id
                                 orderby t.Time, t.Id
                                 select new TradeBase
                                 {
                                     Id = t.Id,
                                     Time = t.Time,
                                     Price = t.Price.Trim(pricePrecision),
                                     Quantity = t.Quantity.Trim(quantityPrecision),
                                     IsBuyerMaker = t.IsBuyerMaker
                                 }).ToList();

                var newTradesCount = newTrades.Count;
                var tradesChartCount = tradesChart.Count;

                if (tradesChartCount >= tradesChartDisplayCount)
                {
                    // For each additional new trade remove the oldest then add the new trade
                    for (int i = 0; i < newTradesCount; i++)
                    {
                        tradesChart.RemoveAt(0);
                        tradesChart.Add(newTrades[i]);
                    }
                }
                else
                {
                    // Get the difference between the number of trades the chart can take and the number it currently holds.
                    var chartDisplayTopUpTradesCount = tradesChartDisplayCount - tradesChartCount;

                    if (newTradesCount > chartDisplayTopUpTradesCount)
                    {
                        // There are more new trades than the chart can take.

                        if (chartDisplayTopUpTradesCount > 0)
                        {
                            // The top up trades can simply be added to the chart as it will take it to the total the chart can hold
                            var chartDisplayTopUpTrades = newTrades.Take(chartDisplayTopUpTradesCount).ToList();
                            tradesChart.AddRange(chartDisplayTopUpTrades);
                        }

                        for (int i = chartDisplayTopUpTradesCount; i < newTradesCount; i++)
                        {
                            // For each additional new trade remove the oldest then add the new trade
                            tradesChart.RemoveAt(0);
                            tradesChart.Add(newTrades[i]);
                        }
                    }
                    else
                    {
                        // Simply add new trades to current list as it wont be more than the total the chart can take.
                        tradesChart.AddRange(newTrades);
                    }
                }

                if (newTradesCount >= tradesDisplayCount)
                {
                    // More new trades than the list can take, only take the newest
                    // trades and create a new instance of the trades list.
                    var tradeBooktrades = newTrades.Skip(newTradesCount - tradesDisplayCount).ToList();

                    // Order by newest to oldest (as it will appear on trade list)
                    trades = new List<TradeBase>(tradeBooktrades.Reverse<TradeBase>().ToList());
                }
                else
                {
                    var tradesCount = trades.Count;

                    // Order the new trades by newest first and oldest last
                    var tradeBooktrades = newTrades.Reverse<TradeBase>().ToList();

                    if ((newTradesCount + tradesCount) > tradesDisplayCount)
                    {
                        // Append to the new trades the balance from the existing trades to make up the trade list limit
                        var balanceTrades = trades.Take(tradesDisplayCount - newTradesCount).ToList();
                        tradeBooktrades.AddRange(balanceTrades);
                    }
                    else
                    {
                        // Simply append the existing trades to the new trades as it will fit in the trade list limit.
                        tradeBooktrades.AddRange(trades);
                    }

                    trades = tradeBooktrades;
                }
            }

            sw.Stop();
            logger.Log($"End TradeUpdate {sw.Elapsed}", Category.Info, Priority.Low);
        }
    }
}