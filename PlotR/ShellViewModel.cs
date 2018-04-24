using Caliburn.Micro;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;

namespace PlotR {
    public class ShellViewModel : Conductor<object>, IShell
    {
        /// <summary>
        /// Initialize.
        /// </summary>
        public ShellViewModel()
        {
            ActivateItem(IoC.Get<HeatmapPlotViewModel>());
        }

    }
}