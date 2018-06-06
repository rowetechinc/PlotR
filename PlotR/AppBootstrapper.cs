namespace PlotR {
    using System;
    using System.Collections.Generic;
    using Caliburn.Micro;

    public class AppBootstrapper : BootstrapperBase {
        SimpleContainer container;

        public AppBootstrapper() {
            Initialize();
        }

        protected override void Configure()
        {
            // Found at https://github.com/gblmarquez/mui-sample-chat/blob/master/src/MuiChat.App/Bootstrapper.cs
            // Allows the CaliburnContentLoader to find the viewmodel based off the view string given
            // Used with ModernUI to navigate between views.
            ViewLocator.NameTransformer.AddRule(
                @"(?<nsbefore>([A-Za-z_]\w*\.)*)?(?<nsvm>ViewModels\.)(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)(?<suffix>ViewModel$)",
                @"${nsbefore}Views.${nsafter}${basename}View",
                @"(([A-Za-z_]\w*\.)*)?ViewModels\.([A-Za-z_]\w*\.)*[A-Za-z_]\w*ViewModel$"
                );

            container = new SimpleContainer();

            base.Configure();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShell, ShellViewModel>();
            container.Singleton<PlotrViewModel, PlotrViewModel>();

            // Register Heatmap
            container.Singleton<HeatmapPlotViewModel, HeatmapPlotViewModel>();
            var heatmap = container.GetInstance<HeatmapPlotViewModel>();        
            container.Instance<IPlotViewModel>(heatmap);                        // Also register as IPlotViewModel
            
            // Register Timeseries 
            container.Singleton<TimeSeriesViewModel, TimeSeriesViewModel>();
            var timeseries = container.GetInstance<TimeSeriesViewModel>();
            container.Instance<IPlotViewModel>(timeseries);                     // Also register as IPlotViewModel

            // Register Shiptrack
            container.Singleton<ShipTrackPlotViewModel, ShipTrackPlotViewModel>();
            var shiptrack = container.GetInstance<ShipTrackPlotViewModel>();
            container.Instance<IPlotViewModel>(shiptrack);                      // Also register as IPlotViewModel
        }

        protected override object GetInstance(Type service, string key) {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service) {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance) {
            container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e) {
            DisplayRootViewFor<IShell>();
        }

    }
}