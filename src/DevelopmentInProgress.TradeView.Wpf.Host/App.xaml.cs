﻿using CommonServiceLocator;
using DevelopmentInProgress.TradeView.Core.Interfaces;
using DevelopmentInProgress.TradeView.Data;
using DevelopmentInProgress.TradeView.Data.File;
using DevelopmentInProgress.TradeView.Service;
using DevelopmentInProgress.TradeView.Wpf.Common.Cache;
using DevelopmentInProgress.TradeView.Wpf.Common.Chart;
using DevelopmentInProgress.TradeView.Wpf.Common.Helpers;
using DevelopmentInProgress.TradeView.Wpf.Common.Services;
using DevelopmentInProgress.TradeView.Wpf.Common.ViewModel;
using DevelopmentInProgress.TradeView.Wpf.Configuration.Utility;
using DevelopmentInProgress.TradeView.Wpf.Host.Controller.Context;
using DevelopmentInProgress.TradeView.Wpf.Host.Controller.Navigation;
using DevelopmentInProgress.TradeView.Wpf.Host.Controller.RegionAdapters;
using DevelopmentInProgress.TradeView.Wpf.Host.Controller.View;
using DevelopmentInProgress.TradeView.Wpf.Host.Logger;
using DevelopmentInProgress.TradeView.Wpf.Strategies.Utility;
using DevelopmentInProgress.TradeView.Wpf.Trading.ViewModel;
using DevelopmentInProgress.TradeView.Wpf.Host.Controller.ViewModel;
using Prism.Ioc;
using Prism.Logging;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using Serilog;
using System.IO;
using System.Windows;
using Xceed.Wpf.AvalonDock;

namespace DevelopmentInProgress.TradeView.Wpf.Host
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override IModuleCatalog CreateModuleCatalog()
        {
            using (Stream xamlStream = File.OpenRead("Configuration/ModuleCatalog.xaml"))
            {
                var moduleCatalog = ModuleCatalog.CreateFromXaml(xamlStream);
                return moduleCatalog;
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Serilog.Core.Logger logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            containerRegistry.RegisterInstance<ILogger>(logger);
            containerRegistry.RegisterSingleton<ILoggerFacade, LoggerFacade>();

            containerRegistry.RegisterSingleton<NavigationManager>();
            containerRegistry.RegisterSingleton<ModulesNavigationView>();
            containerRegistry.RegisterSingleton<ModulesNavigationViewModel>();

            containerRegistry.RegisterSingleton<ModuleNavigator>();
            containerRegistry.Register<IViewContext, ViewContext>();

            containerRegistry.RegisterSingleton<IExchangeApiFactory, ExchangeApiFactory>();
            containerRegistry.Register<IExchangeService, ExchangeService>();

            containerRegistry.Register<ITradeViewConfigurationAccounts, TradeViewConfigurationAccountsFile>();
            containerRegistry.Register<ITradeViewConfigurationStrategy, TradeViewConfigurationStrategyFile>();
            containerRegistry.Register<ITradeViewConfigurationServer, TradeViewConfigurationServerFile>();

            containerRegistry.Register<IAccountsService, AccountsService>();
            containerRegistry.Register<IStrategyService, StrategyService>();
            containerRegistry.Register<ITradeServerService, TradeServerService>();

            containerRegistry.Register<IWpfExchangeService, WpfExchangeService>();
            containerRegistry.Register<ISymbolsCache, SymbolsCache>();
            containerRegistry.RegisterSingleton<ISymbolsCacheFactory, SymbolsCacheFactory>();
            containerRegistry.RegisterSingleton<IServerMonitorCache, ServerMonitorCache>();

            containerRegistry.RegisterSingleton<IOrderBookHelperFactory, OrderBookHelperFactory>();
            containerRegistry.RegisterSingleton<ITradeHelperFactory, TradeHelperFactory>();
            containerRegistry.RegisterSingleton<IHelperFactoryContainer, HelperFactoryContainer>();
            containerRegistry.RegisterSingleton<IChartHelper, ChartHelper>();

            containerRegistry.Register<OrdersViewModel>();
            containerRegistry.Register<AccountViewModel>();

            containerRegistry.Register<SymbolsViewModel>();
            containerRegistry.Register<TradeViewModel>();

            containerRegistry.Register<IStrategyFileManager, StrategyFileManager>();
            containerRegistry.Register<ISymbolsLoader, SymbolsLoader>();

            containerRegistry.Register<IStrategyAssemblyManager, StrategyAssemblyManager>();

            containerRegistry.Register<Strategies.ViewModel.SymbolsViewModel>();
            containerRegistry.Register<Strategies.ViewModel.StrategyParametersViewModel>();
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            regionAdapterMappings.RegisterMapping(typeof(DockingManager), new DockingManagerRegionAdapter(ServiceLocator.Current.GetInstance<IRegionBehaviorFactory>()));
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void InitializeShell(Window shell)
        {
            var modulesNavigationViewModel = Container.Resolve<ModulesNavigationViewModel>();
            ((Shell)shell).ModulesNavigationViewModel = modulesNavigationViewModel;

            Current.MainWindow = shell;
            Current.MainWindow.WindowState = WindowState.Maximized;
            Current.MainWindow.Show();
        }
    }
}
