﻿using DevelopmentInProgress.MarketView.Interface.Extensions;
using DevelopmentInProgress.Wpf.Common.Command;
using DevelopmentInProgress.Wpf.Common.ViewModel;
using DevelopmentInProgress.Wpf.Common.Model;
using DevelopmentInProgress.Wpf.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System;
using DevelopmentInProgress.Wpf.Common.Events;
using Prism.Logging;
using DevelopmentInProgress.Wpf.Configuration.Utility;
using DevelopmentInProgress.MarketView.Interface.Model;

namespace DevelopmentInProgress.Wpf.Configuration.ViewModel
{
    public class StrategyViewModel : BaseViewModel
    {
        private IStrategyService strategyService;
        private IStrategyAssemblyManager strategyAssemblyManager;
        private Strategy strategy;
        bool disposed = false;

        public StrategyViewModel(Strategy strategy, IStrategyService strategyService, IStrategyAssemblyManager strategyAssemblyManager, ILoggerFacade logger)
            : base(logger)
        {
            this.strategy = strategy;
            this.strategyService = strategyService;
            this.strategyAssemblyManager = strategyAssemblyManager;

            AddStrategySubscriptionCommand = new ViewModelCommand(AddStrategySubscription);
            DeleteStrategySubscriptionCommand = new ViewModelCommand(DeleteStrategySubscription);
            DeleteStrategyDependencyCommand = new ViewModelCommand(DeleteStrategyDependency);
            AddParameterSchemaCommand = new ViewModelCommand(AddParameterSchema);

            OnPropertyChanged(string.Empty);
        }

        public event EventHandler<StrategyEventArgs> OnStrategyNotification;

        public ICommand AddStrategySubscriptionCommand { get; set; }
        public ICommand DeleteStrategySubscriptionCommand { get; set; }
        public ICommand DeleteStrategyDependencyCommand { get; set; }
        public ICommand AddParameterSchemaCommand { get; set; }

        public Strategy Strategy
        {
            get { return strategy; }
            set
            {
                if (strategy != value)
                {
                    strategy = value;
                    OnPropertyChanged("Strategy");
                }
            }
        }

        public IEnumerable<string> TargetAssembly
        {
            set
            {
                if (Strategy != null)
                {
                    if (value != null
                        && value.Count() > 0)
                    {
                        var file = value.First();
                        Strategy.TargetAssembly = new StrategyFile { File = file };

                        if(!Strategy.Dependencies.Any(d => d.File.Equals(file)))
                        {
                            Strategy.Dependencies.Insert(0, new StrategyFile { File = file, FileType = StrategyFileType.StrategyFile });
                        }
                    }
                }
            }
        }

        public IEnumerable<string> Dependencies
        {
            set
            {
                if (Strategy != null)
                {
                    if (value != null
                        && value.Count() > 0)
                    {
                        foreach (string file in value)
                        {
                            Strategy.Dependencies.Insert(0, new StrategyFile { File = file, FileType = StrategyFileType.StrategyFile });
                        }
                    }
                }
            }
        }

        public IEnumerable<string> DisplayAssembly
        {
            set
            {
                if (Strategy != null)
                {
                    if (value != null
                        && value.Count() > 0)
                    {
                        var file = value.First();
                        Strategy.DisplayAssembly = new StrategyFile { File = file };

                        if (!Strategy.DisplayDependencies.Any(d => d.File.Equals(file)))
                        {
                            Strategy.DisplayDependencies.Insert(0, new StrategyFile { File = file, FileType = StrategyFileType.DisplayFile });
                        }
                    }
                }
            }
        }

        public IEnumerable<string> DisplayDependencies
        {
            set
            {
                if (Strategy != null)
                {
                    if (value != null
                        && value.Count() > 0)
                    {
                        foreach (string file in value)
                        {
                            Strategy.DisplayDependencies.Insert(0, new StrategyFile { File = file, FileType = StrategyFileType.DisplayFile });
                        }
                    }
                }
            }
        }

        public string[] Exchanges
        {
            get { return ExchangeExtensions.Exchanges(); }
        }

        public string[] CandlestickIntervals
        {
            get { return CandlestickIntervalExtensions.GetCandlestickIntervalNames(); }
        }
        
        public override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose stuff...
            }

            disposed = true;
        }

        private async void AddStrategySubscription(object param)
        {
            if (Strategy == null
                || string.IsNullOrEmpty(param.ToString()))
            {
                return;
            }

            var symbol = param.ToString();

            try
            {
                Strategy.StrategySubscriptions.Insert(0, new StrategySubscription { Symbol = symbol });
                await strategyService.SaveStrategy(Strategy);
            }
            catch (Exception ex)
            {
                OnStrategyException(ex);
            }
        }

        private async void DeleteStrategySubscription(object param)
        {
            if (Strategy == null)
            {
                return;
            }

            var subscription = param as StrategySubscription;
            if (subscription != null)
            {
                try
                {
                    Strategy.StrategySubscriptions.Remove(subscription);
                    await strategyService.SaveStrategy(Strategy);
                }
                catch (Exception ex)
                {
                    OnStrategyException(ex);
                }
            }
        }

        private async void DeleteStrategyDependency(object param)
        {
            if (Strategy == null)
            {
                return;
            }

            var file = param as StrategyFile;
            if (file != null)
            {
                try
                {
                    if(file.FileType.Equals(StrategyFileType.StrategyFile))
                    {
                        Strategy.Dependencies.Remove(file);
                    }
                    else if(file.FileType.Equals(StrategyFileType.DisplayFile))
                    {
                        Strategy.DisplayDependencies.Remove(file);
                    }

                    await strategyService.SaveStrategy(Strategy);
                }
                catch (Exception ex)
                {
                    OnStrategyException(ex);
                }
            }
        }

        private async void AddParameterSchema(object param)
        {
            if(Strategy == null)
            {
                return;
            }

            if(Strategy.TargetAssembly == null
                || string.IsNullOrWhiteSpace(Strategy.TargetAssembly.File))
            {
                OnStrategyException(new Exception("Target assembly must be specified."));
                return;
            }

            try
            {
                var strategyTypeJson = strategyAssemblyManager.GetStrategyTypeAsJson(strategy.TargetAssembly);

                if(string.IsNullOrWhiteSpace(strategyTypeJson))
                {
                    return;
                }

                Strategy.Parameters = strategyTypeJson;

                await strategyService.SaveStrategy(Strategy);
            }
            catch (Exception ex)
            {
                OnStrategyException(ex);
            }
        }

        private void OnStrategyException(Exception exception)
        {
            var onStrategyNotification = OnStrategyNotification;
            onStrategyNotification?.Invoke(this, new StrategyEventArgs { Value = Strategy, Exception = exception });
        }
    }
}