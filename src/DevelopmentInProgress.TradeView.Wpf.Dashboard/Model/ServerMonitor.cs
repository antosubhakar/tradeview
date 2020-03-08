﻿using DevelopmentInProgress.TradeView.Wpf.Common.Model;
using System;
using System.Collections.ObjectModel;

namespace DevelopmentInProgress.TradeView.Wpf.Dashboard.Model
{
    public class ServerMonitor : EntityBase
    {
        private string name;
        private string url;
        private int maxDegreeOfParallelism;
        private string startedBy;
        private string stoppedBy;
        private DateTime started;
        private DateTime stopped;
        private ObservableCollection<Strategy> strategies;

        public ServerMonitor()
        {
            Strategies = new ObservableCollection<Strategy>();
        }

        public string Name 
        {
            get { return name; }
            set
            {
                if(name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string Url
        {
            get { return url; }
            set
            {
                if (url != value)
                {
                    url = value;
                    OnPropertyChanged("Url");
                }
            }
        }

        public int MaxDegreeOfParallelism
        {
            get { return maxDegreeOfParallelism; }
            set
            {
                if (maxDegreeOfParallelism != value)
                {
                    maxDegreeOfParallelism = value;
                    OnPropertyChanged("MaxDegreeOfParallelism");
                }
            }
        }

        public string StartedBy
        {
            get { return startedBy; }
            set
            {
                if (startedBy != value)
                {
                    startedBy = value;
                    OnPropertyChanged("StartedBy");
                }
            }
        }

        public string StoppedBy
        {
            get { return stoppedBy; }
            set
            {
                if (stoppedBy != value)
                {
                    stoppedBy = value;
                    OnPropertyChanged("StoppedBy");
                }
            }
        }

        public DateTime Started
        {
            get { return started; }
            set
            {
                if (started != value)
                {
                    started = value;
                    OnPropertyChanged("Started");
                }
            }
        }

        public DateTime Stopped
        {
            get { return stopped; }
            set
            {
                if (stopped != value)
                {
                    stopped = value;
                    OnPropertyChanged("Stopped");
                }
            }
        }

        public ObservableCollection<Strategy> Strategies
        {
            get { return strategies; }
            set
            {
                if (strategies != value)
                {
                    strategies = value;
                    OnPropertyChanged("Strategies");
                }
            }
        }
    }
}
