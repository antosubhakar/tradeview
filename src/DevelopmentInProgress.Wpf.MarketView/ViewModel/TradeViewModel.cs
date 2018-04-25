﻿using DevelopmentInProgress.MarketView.Interface.Helpers;
using DevelopmentInProgress.Wpf.Host.ViewModel;
using DevelopmentInProgress.Wpf.MarketView.Events;
using DevelopmentInProgress.Wpf.MarketView.Model;
using DevelopmentInProgress.Wpf.MarketView.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Interface = DevelopmentInProgress.MarketView.Interface.Model;

namespace DevelopmentInProgress.Wpf.MarketView.ViewModel
{
    public class TradeViewModel : BaseViewModel
    {
        private List<Symbol> symbols;
        private Symbol selectedSymbol;
        private Account account;
        private AccountBalance baseAccountBalance;
        private AccountBalance quoteAccountBalance;
        private string selectedOrderType;
        private decimal quantity;
        private decimal price;
        private decimal stopPrice;
        private bool disposed;
        private bool isLoading;

        public TradeViewModel(IExchangeService exchangeService)
            : base(exchangeService)
        {
            BuyCommand = new ViewModelCommand(Buy);
            SellCommand = new ViewModelCommand(Sell);
            BuyQuantityCommand = new ViewModelCommand(BuyQuantity);
            SellQuantityCommand = new ViewModelCommand(SellQuantity);
            OnPropertyChanged("");
        }

        public event EventHandler<TradeEventArgs> OnTradeNotification;
        
        public ICommand BuyCommand { get; set; }
        public ICommand SellCommand { get; set; }
        public ICommand BuyQuantityCommand { get; set; }
        public ICommand SellQuantityCommand { get; set; }

        public Account Account
        {
            get { return account; }
            set
            {
                if (account != value)
                {
                    account = value;
                }
            }
        }

        public AccountBalance BaseAccountBalance
        {
            get { return baseAccountBalance; }
            set
            {
                if (baseAccountBalance != value)
                {
                    baseAccountBalance = value;
                    OnPropertyChanged("HasBaseBalance");
                    OnPropertyChanged("BaseAccountBalance");
                }
            }
        }

        public AccountBalance QuoteAccountBalance
        {
            get { return quoteAccountBalance; }
            set
            {
                if (quoteAccountBalance != value)
                {
                    quoteAccountBalance = value;
                    OnPropertyChanged("HasQuoteBalance");
                    OnPropertyChanged("QuoteAccountBalance");
                }
            }
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (isLoading != value)
                {
                    isLoading = value;
                    OnPropertyChanged("IsLoading");
                }
            }
        }

        public List<Symbol> Symbols
        {
            get { return symbols; }
            set
            {
                if (symbols != value)
                {
                    symbols = value;
                    OnPropertyChanged("Symbols");
                }
            }
        }

        public Symbol SelectedSymbol
        {
            get { return selectedSymbol; }
            set
            {
                if (selectedSymbol != value)
                {
                    selectedSymbol = value;
                    
                    if(selectedSymbol != null)
                    {
                        Quantity = 0;
                        Price = SelectedSymbol.SymbolStatistics.LastPrice;
                        StopPrice = SelectedSymbol.SymbolStatistics.LastPrice;
                        BaseAccountBalance = Account?.Balances.SingleOrDefault(ab => ab.Asset.Equals(selectedSymbol.BaseAsset.Symbol));
                        QuoteAccountBalance = Account?.Balances.SingleOrDefault(ab => ab.Asset.Equals(selectedSymbol.QuoteAsset.Symbol));
                    }
                    else
                    {
                        Price = 0;
                        StopPrice = 0;
                        Quantity = 0;
                    }

                    OnPropertyChanged("SelectedSymbol");
                }
            }
        }

        public decimal Quantity
        {
            get { return quantity; }
            set
            {
                if (quantity != value)
                {
                    quantity = value;
                    OnPropertyChanged("Quantity");
                }
            }
        }

        public decimal Price
        {
            get { return price; }
            set
            {
                if (price != value)
                {
                    price = value;
                    OnPropertyChanged("Price");
                }
            }
        }

        public decimal StopPrice
        {
            get { return stopPrice; }
            set
            {
                if (stopPrice != value)
                {
                    stopPrice = value;
                    OnPropertyChanged("StopPrice");
                }
            }
        }

        public string SelectedOrderType
        {
            get { return selectedOrderType; }
            set
            {
                if (selectedOrderType != value)
                {
                    selectedOrderType = value;

                    if (SelectedSymbol != null)
                    {
                        Price = SelectedSymbol.SymbolStatistics.LastPrice;
                        StopPrice = SelectedSymbol.SymbolStatistics.LastPrice;
                    }

                    OnPropertyChanged("IsPriceEditable");
                    OnPropertyChanged("IsMarketPrice");
                    OnPropertyChanged("IsStopLoss");
                    OnPropertyChanged("SelectedOrderType");
                }
            }
        }

        public bool IsPriceEditable
        {
            get
            {
                if (IsLoading)
                {
                    return !IsLoading;
                }

                return !OrderHelper.IsMarketOrder(SelectedOrderType);
            }
        }

        public bool IsMarketPrice
        {
            get
            {
                if (IsLoading)
                {
                    return !IsLoading;
                }

                return OrderHelper.IsMarketOrder(SelectedOrderType);
            }
        }

        public bool IsStopLoss
        {
            get
            {
                if (IsLoading)
                {
                    return !IsLoading;
                }

                return OrderHelper.IsStopLoss(SelectedOrderType);
            }
        }

        public bool HasBaseBalance
        {
            get
            {
                if (BaseAccountBalance != null
                    && BaseAccountBalance.Free > 0)
                {
                    return true;
                }

                return false;
            }
        }

        public bool HasQuoteBalance
        {
            get
            {
                if (QuoteAccountBalance != null
                    && QuoteAccountBalance.Free > 0)
                {
                    return true;
                }

                return false;
            }
        }

        public string[] OrderTypes
        {
            get { return OrderHelper.OrderTypes(); }
        }
        
        public void SetSymbols(List<Symbol> symbols)
        {
            Symbols = symbols;
        }

        public void SetAccount(Account account, AccountBalance selectedAsset)
        {
            if (Account == null
                || !Account.ApiKey.Equals(account.ApiKey))
            {
                Account = account;
                SelectedOrderType = OrderHelper.GetOrderTypeName(Interface.OrderType.Limit);
            }

            if (selectedAsset != null)
            {
                SelectedSymbol = Symbols.FirstOrDefault(s => s.BaseAsset.Symbol.Equals(selectedAsset.Asset));
            }
        }

        public override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
            }

            disposed = true;
        }

        private void Buy(object param)
        {
            SendClientOrder(Interface.OrderSide.Buy);
        }

        private void Sell(object param)
        {
            SendClientOrder(Interface.OrderSide.Sell);
        }

        private async void SendClientOrder(Interface.OrderSide orderSide)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedOrderType))
                {
                    throw new Exception("Order not valid: No order type.");
                }

                var clientOrder = new Interface.ClientOrder
                {
                    Symbol = SelectedSymbol?.Name,
                    Type = OrderHelper.GetOrderType(SelectedOrderType),
                    Side = orderSide,
                    Quantity = Quantity,
                    Price = Price,
                    StopPrice = StopPrice
                };

                OrderHelper.ValidateClientOrder(clientOrder);

                var order = await ExchangeService.PlaceOrder(Account.AccountInfo.User, clientOrder);
            }
            catch (Exception e)
            {
                OnException(e);
            }
        }

        private void BuyQuantity(object param)
        {
            SetQuantity(param.ToString(), QuoteAccountBalance.Free);
        }

        private void SellQuantity(object param)
        {
            SetQuantity(param.ToString(), BaseAccountBalance.Free);
        }

        private void SetQuantity(string percentage, decimal total)
        {
            decimal percent;
            if (Decimal.TryParse(percentage, out percent))
            {
                Quantity = (percent / 100) * total;
            }
        }

        private void OnException(Exception exception)
        {
            var onTradeNotification = OnTradeNotification;
            onTradeNotification?.Invoke(this, new TradeEventArgs { Exception = exception });
        }
    }
}
