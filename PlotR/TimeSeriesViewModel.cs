using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{
    class TimeSeriesViewModel : PlotViewModel
    {
        #region Class

        /// <summary>
        /// Info on a series.
        /// </summary>
        public class SeriesInfo
        {
            public PlotDataType PlotType { get; set; }

            /// <summary>
            /// Color of the line.
            /// </summary>
            public OxyColor Color { get; set; }

            /// <summary>
            /// Beam number of the line.
            /// </summary>
            public int Beam { get; set; }

            /// <summary>
            /// Bin number of ht line
            /// </summary>
            public int Bin { get; set; }

            /// <summary>
            /// Initialize.
            /// </summary>
            public SeriesInfo()
            {
                PlotType = PlotDataType.Magnitude;
                Color = OxyColors.YellowGreen;
                Beam = 0;
                Bin = 0;
            }

            public override string ToString()
            {
                return String.Format("{0} Bin:{1} Beam:{2}", PlotType, Bin, Beam);
            }
        }


        /// <summary>
        /// Class to hold the plot data.
        /// </summary>
        public class SeriesData
        {
            /// <summary>
            /// Info on the series.
            /// </summary>
            public SeriesInfo Info { get; set; }

            /// <summary>
            /// Profile data to plot.
            /// </summary>
            public List<DataPoint> Data { get; set; }

            /// <summary>
            /// Initialize the object.
            /// </summary>
            public SeriesData()
            {
                // Initialize
                Info = new SeriesInfo();
                Data = new List<DataPoint>();
            }
        }

        #endregion

        #region Properies

        #region Series List

        /// <summary>
        /// List of series displayed.
        /// </summary>
        public BindingList<SeriesInfo> SeriesList { get; set; }

        /// <summary>
        /// Series Data to remove.
        /// </summary>
        private SeriesInfo _SelectedSeries;
        /// <summary>
        /// Series Data to remove.
        /// </summary>
        public SeriesInfo SelectedSeries
        {
            get { return _SelectedSeries; }
            set
            {
                _SelectedSeries = value;
                NotifyOfPropertyChange(() => SelectedSeries);
            }
        }

        #endregion

        #region Add Series

        /// <summary>
        /// List of available plot types.
        /// </summary>
        public BindingList<PlotDataType> PlotTypeList { get; set; }

        ///// <summary>
        ///// Selected plot type.
        ///// </summary>
        //private PlotDataType _SelectedPlotType;
        ///// <summary>
        ///// Selected plot type.
        ///// </summary>
        //public PlotDataType SelectedPlotType
        //{
        //    get { return _SelectedPlotType; }
        //    set
        //    {
        //        _SelectedPlotType = value;
        //        NotifyOfPropertyChange(() => SelectedPlotType);
        //    }
        //}

        /// <summary>
        /// List of colors.
        /// </summary>
        public BindingList<OxyColor> SeriesColorList { get; set; }

        /// <summary>
        /// Selected series color.
        /// </summary>
        private OxyColor _SelectedSeriesColor;
        /// <summary>
        /// Selected series color.
        /// </summary>
        public OxyColor SelectedSeriesColor
        {
            get { return _SelectedSeriesColor; }
            set
            {
                _SelectedSeriesColor = value;
                NotifyOfPropertyChange(() => SelectedSeriesColor);
            }
        }

        /// <summary>
        /// Selected bin.
        /// </summary>
        private int _SelectedBin;
        /// <summary>
        /// Selected bin.
        /// </summary>
        public int SelectedBin
        {
            get { return _SelectedBin; }
            set
            {
                _SelectedBin = value;
                NotifyOfPropertyChange(() => SelectedBin);
            }
        }

        /// <summary>
        /// Selected beam.
        /// </summary>
        private int _SelectedBeam;
        /// <summary>
        /// Selected beam.
        /// </summary>
        public int SelectedBeam
        {
            get { return _SelectedBeam; }
            set
            {
                _SelectedBeam = value;
                NotifyOfPropertyChange(() => SelectedBeam);
            }
        }



        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Add a series to the plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddSeriesCommand { get; private set; }

        /// <summary>
        /// Remove a Series from the plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RemoveSeriesCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initialize the VM.
        /// </summary>
        public TimeSeriesViewModel()
        {
            // Create the plot
            Plot = CreatePlot();

            SelectedBeam = 0;
            SelectedBin = 0;

            // Selected Plot Type
            //PlotTypeList = Enum.GetValues(typeof(PlotDataType)).Cast<PlotDataType>().ToList();
            SelectedPlotType = PlotDataType.Magnitude;
            IsMagnitude = true;

            // Status
            StatusMsg = "Open a DB file...";
            StatusProgress = 0;
            StatusProgressMax = 100;

            // Add a base set of data to the time series
            SetupLists();

            SeriesList.Add(new SeriesInfo() { PlotType = PlotDataType.Magnitude, Beam = 0, Bin = 0, Color = OxyColors.DarkGoldenrod });
            SeriesList.Add(new SeriesInfo() { PlotType = PlotDataType.Magnitude, Beam = 0, Bin = 1, Color = OxyColors.Beige });
            SeriesList.Add(new SeriesInfo() { PlotType = PlotDataType.Magnitude, Beam = 0, Bin = 2, Color = OxyColors.BlueViolet });
            SeriesList.Add(new SeriesInfo() { PlotType = PlotDataType.Magnitude, Beam = 0, Bin = 3, Color = OxyColors.DarkSlateBlue });

            // Add Series Command commands
            this.AddSeriesCommand = ReactiveCommand.Create(() => AddSeries());

            // Add Series Command commands
            this.RemoveSeriesCommand = ReactiveCommand.Create(() => RemoveSeries());
        }

        #region Setup List

        /// <summary>
        /// Setup the lists.
        /// </summary>
        private void SetupLists()
        {
            PlotTypeList = new BindingList<PlotDataType>();
            foreach(PlotDataType plotType in Enum.GetValues(typeof(PlotDataType)))
            {
                PlotTypeList.Add(plotType);
            }

            SeriesList = new BindingList<SeriesInfo>();

            SeriesColorList = new BindingList<OxyColor>();
            SeriesColorList.Add(OxyColors.AliceBlue);
            SeriesColorList.Add(OxyColors.AntiqueWhite);
            SeriesColorList.Add(OxyColors.Aqua);
            SeriesColorList.Add(OxyColors.Aquamarine);
            SeriesColorList.Add(OxyColors.Azure);
            SeriesColorList.Add(OxyColors.Beige);
            SeriesColorList.Add(OxyColors.Bisque);
            SeriesColorList.Add(OxyColors.Black);
            SeriesColorList.Add(OxyColors.BlanchedAlmond);
            SeriesColorList.Add(OxyColors.Blue);
            SeriesColorList.Add(OxyColors.BlueViolet);
            SeriesColorList.Add(OxyColors.Brown);
            SeriesColorList.Add(OxyColors.BurlyWood);
            SeriesColorList.Add(OxyColors.CadetBlue);
            SeriesColorList.Add(OxyColors.Chartreuse);
            SeriesColorList.Add(OxyColors.Chocolate);
            SeriesColorList.Add(OxyColors.Coral);
            SeriesColorList.Add(OxyColors.CornflowerBlue);
            SeriesColorList.Add(OxyColors.Cornsilk);
            SeriesColorList.Add(OxyColors.Crimson);
            SeriesColorList.Add(OxyColors.Cyan);
            SeriesColorList.Add(OxyColors.DarkBlue);
            SeriesColorList.Add(OxyColors.DarkCyan);
            SeriesColorList.Add(OxyColors.DarkGoldenrod);
            SeriesColorList.Add(OxyColors.DarkGray);
            SeriesColorList.Add(OxyColors.DarkGreen);
            SeriesColorList.Add(OxyColors.DarkKhaki);
            SeriesColorList.Add(OxyColors.DarkMagenta);
            SeriesColorList.Add(OxyColors.DarkOliveGreen);
            SeriesColorList.Add(OxyColors.DarkOrange);
            SeriesColorList.Add(OxyColors.DarkOrchid);
            SeriesColorList.Add(OxyColors.DarkRed);
            SeriesColorList.Add(OxyColors.DarkSalmon);
            SeriesColorList.Add(OxyColors.DarkSeaGreen);
            SeriesColorList.Add(OxyColors.DarkSlateBlue);
            SeriesColorList.Add(OxyColors.DarkSlateGray);
            SeriesColorList.Add(OxyColors.DarkTurquoise);
            SeriesColorList.Add(OxyColors.DarkViolet);
            SeriesColorList.Add(OxyColors.DeepPink);
            SeriesColorList.Add(OxyColors.DeepSkyBlue);
            SeriesColorList.Add(OxyColors.DimGray);
            SeriesColorList.Add(OxyColors.DodgerBlue);
            SeriesColorList.Add(OxyColors.Firebrick);
            SeriesColorList.Add(OxyColors.FloralWhite);
            SeriesColorList.Add(OxyColors.ForestGreen);
            SeriesColorList.Add(OxyColors.Fuchsia);
            SeriesColorList.Add(OxyColors.Gainsboro);
            SeriesColorList.Add(OxyColors.GhostWhite);
            SeriesColorList.Add(OxyColors.Gold);
            SeriesColorList.Add(OxyColors.Goldenrod);
            SeriesColorList.Add(OxyColors.Gray);
            SeriesColorList.Add(OxyColors.Green);
            SeriesColorList.Add(OxyColors.GreenYellow);
            SeriesColorList.Add(OxyColors.Honeydew);
            SeriesColorList.Add(OxyColors.HotPink);
            SeriesColorList.Add(OxyColors.IndianRed);
            SeriesColorList.Add(OxyColors.Indigo);
            SeriesColorList.Add(OxyColors.Ivory);
            SeriesColorList.Add(OxyColors.Khaki);
            SeriesColorList.Add(OxyColors.Lavender);
            SeriesColorList.Add(OxyColors.LavenderBlush);
            SeriesColorList.Add(OxyColors.LawnGreen);
            SeriesColorList.Add(OxyColors.LemonChiffon);
            SeriesColorList.Add(OxyColors.LightBlue);
            SeriesColorList.Add(OxyColors.LightCoral);
            SeriesColorList.Add(OxyColors.LightCyan);
            SeriesColorList.Add(OxyColors.LightGoldenrodYellow);
            SeriesColorList.Add(OxyColors.LightGray);
            SeriesColorList.Add(OxyColors.LightGreen);
            SeriesColorList.Add(OxyColors.LightPink);
            SeriesColorList.Add(OxyColors.LightSalmon);
            SeriesColorList.Add(OxyColors.LightSeaGreen);
            SeriesColorList.Add(OxyColors.LightSkyBlue);
            SeriesColorList.Add(OxyColors.LightSlateGray);
            SeriesColorList.Add(OxyColors.LightSteelBlue);
            SeriesColorList.Add(OxyColors.LightYellow);
            SeriesColorList.Add(OxyColors.Lime);
            SeriesColorList.Add(OxyColors.LimeGreen);
            SeriesColorList.Add(OxyColors.Linen);
            SeriesColorList.Add(OxyColors.Magenta);
            SeriesColorList.Add(OxyColors.Maroon);
            SeriesColorList.Add(OxyColors.MediumAquamarine);
            SeriesColorList.Add(OxyColors.MediumBlue);
            SeriesColorList.Add(OxyColors.MediumOrchid);
            SeriesColorList.Add(OxyColors.MediumPurple);
            SeriesColorList.Add(OxyColors.MediumSeaGreen);
            SeriesColorList.Add(OxyColors.MediumSlateBlue);
            SeriesColorList.Add(OxyColors.MediumSpringGreen);
            SeriesColorList.Add(OxyColors.MediumTurquoise);
            SeriesColorList.Add(OxyColors.MediumVioletRed);
            SeriesColorList.Add(OxyColors.MidnightBlue);
            SeriesColorList.Add(OxyColors.MintCream);
            SeriesColorList.Add(OxyColors.MistyRose);
            SeriesColorList.Add(OxyColors.Moccasin);
            SeriesColorList.Add(OxyColors.NavajoWhite);
            SeriesColorList.Add(OxyColors.Navy);
            SeriesColorList.Add(OxyColors.OldLace);
            SeriesColorList.Add(OxyColors.Olive);
            SeriesColorList.Add(OxyColors.OliveDrab);
            SeriesColorList.Add(OxyColors.Orange);
            SeriesColorList.Add(OxyColors.OrangeRed);
            SeriesColorList.Add(OxyColors.Orchid);
            SeriesColorList.Add(OxyColors.PaleGoldenrod);
            SeriesColorList.Add(OxyColors.PaleGreen);
            SeriesColorList.Add(OxyColors.PaleTurquoise);
            SeriesColorList.Add(OxyColors.PaleVioletRed);
            SeriesColorList.Add(OxyColors.PapayaWhip);
            SeriesColorList.Add(OxyColors.PeachPuff);
            SeriesColorList.Add(OxyColors.Peru);
            SeriesColorList.Add(OxyColors.Pink);
            SeriesColorList.Add(OxyColors.Plum);
            SeriesColorList.Add(OxyColors.PowderBlue);
            SeriesColorList.Add(OxyColors.Purple);
            SeriesColorList.Add(OxyColors.Red);
            SeriesColorList.Add(OxyColors.RosyBrown);
            SeriesColorList.Add(OxyColors.RoyalBlue);
            SeriesColorList.Add(OxyColors.SaddleBrown);
            SeriesColorList.Add(OxyColors.Salmon);
            SeriesColorList.Add(OxyColors.SandyBrown);
            SeriesColorList.Add(OxyColors.SeaGreen);
            SeriesColorList.Add(OxyColors.SeaShell);
            SeriesColorList.Add(OxyColors.Sienna);
            SeriesColorList.Add(OxyColors.Silver);
            SeriesColorList.Add(OxyColors.SkyBlue);
            SeriesColorList.Add(OxyColors.SlateBlue);
            SeriesColorList.Add(OxyColors.SlateGray);
            SeriesColorList.Add(OxyColors.Snow);
            SeriesColorList.Add(OxyColors.SpringGreen);
            SeriesColorList.Add(OxyColors.SteelBlue);
            SeriesColorList.Add(OxyColors.Tan);
            SeriesColorList.Add(OxyColors.Teal);
            SeriesColorList.Add(OxyColors.Thistle);
            SeriesColorList.Add(OxyColors.Tomato);
            SeriesColorList.Add(OxyColors.Transparent);
            SeriesColorList.Add(OxyColors.Turquoise);
            SeriesColorList.Add(OxyColors.Violet);
            SeriesColorList.Add(OxyColors.Wheat);
            SeriesColorList.Add(OxyColors.White);
            SeriesColorList.Add(OxyColors.WhiteSmoke);
            SeriesColorList.Add(OxyColors.Yellow);
            SeriesColorList.Add(OxyColors.YellowGreen);

            // Set the default selected color
            if (SeriesColorList.Count > 0)
            {
                SelectedSeriesColor = SeriesColorList.First<OxyColor>();
            }
        }

        #endregion

        #region Add Series

        /// <summary>
        /// Add a series to the list.
        /// </summary>
        private void AddSeries()
        {
            // Add the series to the list
            SeriesList.Add(new SeriesInfo() { PlotType = SelectedPlotType, Beam = SelectedBeam, Bin = SelectedBin, Color = SelectedSeriesColor });

            // Redraw the plot
            ReplotData();
        }

        #endregion

        #region Remove Series

        /// <summary>
        /// Remove the series from the plot.
        /// </summary>
        private void RemoveSeries()
        {
            // Remove the series from the list
            SeriesList.Remove(_SelectedSeries);

            // Replot the data
            ReplotData();
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project.  Use the selected min and max index to select the ensemble range to display.
        /// </summary>
        /// <param name="fileName">Project file path.</param>
        /// <param name="minIndex">Minimum Ensemble index.</param>
        /// <param name="maxIndex">Maximum Ensemble index.</param>
        public override void LoadProject(string fileName, int minIndex = 0, int maxIndex = 0)
        {
            // Load the base calls
            base.LoadProject(fileName, minIndex, maxIndex);

            // Plot the data
            ReplotData(minIndex, maxIndex);
        }

        #endregion

        #region Create Plot

            /// <summary>
            /// Create the plot.
            /// </summary>
            /// <returns></returns>
            private ViewResolvingPlotModel CreatePlot()
        {
            ViewResolvingPlotModel temp = new ViewResolvingPlotModel();

            //temp.AutoAdjustPlotMargins = false;
            //temp.PlotMargins = new OxyThickness(0, 0, 0, 0);
            //temp.Padding = new OxyThickness(10,0,10,0);

            //temp.TitleFontSize = 10.667;

            //temp.Background = OxyColors.Black;
            //temp.TextColor = OxyColors.White;
            //temp.PlotAreaBorderColor = OxyColors.White;

            // Set the legend position
            temp.IsLegendVisible = true;
            //temp.LegendPosition = LegendPosition.RightTop;
            //temp.LegendPlacement = LegendPlacement.Outside;
            temp.LegendOrientation = LegendOrientation.Vertical;
            //temp.LegendSymbolPlacement = LegendSymbolPlacement.Right;
            //temp.LegendFontSize = 8;   // 10
                                       //temp.LegendItemSpacing = 8;

            // Setup the axis
            temp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                //MajorStep = 1,
                //Minimum = 0,
                //Maximum = _maxDataSets,
                //TicklineColor = OxyColors.White,
                //MajorGridlineStyle = LineStyle.Solid,
               // MinorGridlineStyle = LineStyle.Solid,
               // MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
                //MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
                TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
                MinimumPadding = 0,                                                 // Start at axis edge   
                MaximumPadding = 0,                                                 // Start at axis edge
                //IsAxisVisible = true,
                //MajorStep = 1,
                Unit = "ENS"

            });

            return temp;
        }

        #endregion

        #region Draw Plot

        /// <summary>
        /// Draw the plot based off the settings.
        /// </summary>
        /// <param name="fileName">File name of the project.</param>
        /// <param name="seriesData">Series Data and Info.</param>
        /// <param name="minIndex">Minimum ensemble index to draw.</param>
        /// <param name="maxIndex">Maximum ensemble index to draw.</param>
        protected async void DrawPlot(string fileName, SeriesData seriesData, int minIndex = 0, int maxIndex = 0)
        {
            // Data to get from the project
            SeriesData data = null;

            // Verify a file was given
            if (!string.IsNullOrEmpty(fileName))
            {
                // Verify the file exist
                if (File.Exists(fileName))
                {
                    // Create data Source string
                    string dataSource = string.Format("Data Source={0};Version=3;", fileName);

                    try
                    {
                        // Create a new database connection:
                        using (SQLiteConnection sqlite_conn = new SQLiteConnection(dataSource))
                        {
                            // Open the connection:
                            sqlite_conn.Open();

                            // Get total number of ensembles in the project
                            await Task.Run(() => TotalNumEnsembles = GetNumEnsembles(sqlite_conn));

                            // If this is the first time loading
                            // show the entire plot
                            if (_firstLoad)
                            {
                                _firstLoad = false;
                                minIndex = 1;
                                maxIndex = TotalNumEnsembles;
                            }

                            // Get the data
                            await Task.Run(() => data = GetData(sqlite_conn, seriesData, minIndex, maxIndex));

                            // Close connection
                            sqlite_conn.Close();
                        }
                    }
                    catch (SQLiteException e)
                    {
                        Debug.WriteLine("Error using database", e);
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Error using database", e);
                        return;
                    }

                    // If there is no data, do not plot
                    if (data != null)
                    {
                        // Update status
                        StatusMsg = "Drawing Plot";

                        // Plot the Profile data from the project
                        await Task.Run(() => PlotSeriesData(data));

                        // Reset the axis to set the meters axis
                        await Task.Run(() => Plot.ResetAllAxes());
                    }
                    else
                    {
                        StatusMsg = "No data to plot";
                    }
                }
            }
        }

        #endregion

        #region Plot Series

        /// <summary>
        /// Plot the series data.
        /// </summary>
        /// <param name="data"></param>
        private void PlotSeriesData(SeriesData data)
        {
            // Create the line title
            string title = "";
            switch (data.Info.PlotType)
            {
                case PlotDataType.Magnitude:
                case PlotDataType.Direction:
                    title = string.Format("{0} Bin[{1}]", data.Info.PlotType, data.Info.Bin);
                    break;
                case PlotDataType.Amplitude:
                default:
                    title = string.Format("{0} Bin[{1}] Beam[{2}]", data.Info.PlotType, data.Info.Bin, data.Info.Beam);
                    break;
            }

            // Create a Line series
            LineSeries series = new LineSeries();
            series.Title = title;
            series.Color = data.Info.Color;

            // Add data to the series
            series.Points.AddRange(data.Data);

            lock (Plot.SyncRoot)
            {
                // Add it to the plot
                Plot.Series.Add(series);
            }
            Plot.InvalidatePlot(true);
        }

        #endregion

        #region Replot Data

        public override void ReplotData(PlotDataType eplotDataType)
        {

        }

        /// <summary>
        /// Implement reploting the data.
        /// </summary>
        /// <param name="minIndex">Minimum Index.</param>
        /// <param name="maxIndex">Maximum Index.</param>
        public async override void ReplotData(int minIndex, int maxIndex)
        {
            // Clear the plot
            lock (Plot.SyncRoot)
            {
                // Add it to the plot
                Plot.Series.Clear();
            }
            Plot.InvalidatePlot(true);

            // Plot the data
            foreach (SeriesInfo seriesInfo in SeriesList)
            {
                // Create the series data
                SeriesData seriesData = new SeriesData();
                seriesData.Info = seriesInfo;

                // Replot the data
                if (!string.IsNullOrEmpty(ProjectFilePath))
                {
                    // Draw the line series
                    await Task.Run(() => DrawPlot(ProjectFilePath, seriesData, minIndex, maxIndex));
                }
            }
        }

        /// <summary>
        /// Implement replotting the data.
        /// </summary>
        public override void ReplotData()
        {
            // Replot the data
            ReplotData(0, 0);
        }

        #endregion

        #region Get Data

        /// <summary>
        /// Get the data based off the selected data type.
        /// </summary>
        /// <param name="ePlotDataType">Selected data type.</param>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="seriesData">Series Data and Info.</param>
        /// <param name="minIndex">Minimum Ensemble index.</param>
        /// <param name="maxIndex">Maximum Ensemble index.</param>
        /// <returns>The selected for each ensemble and bin.</returns>
        private SeriesData GetData(SQLiteConnection cnn, SeriesData seriesData, int minIndex = 0, int maxIndex = 0)
        {
            StatusProgressMax = TotalNumEnsembles;
            StatusProgress = 0;

            // Get the data to plot
            return QueryDataFromDb(cnn, seriesData, minIndex, maxIndex);
        }

        /// <summary>
        /// Query the project database for the data to plot.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="seriesData">Series Data and Info.</param>
        /// <param name="minIndex">Minimum index.</param>
        /// <param name="maxIndex">Maximum index.</param>
        /// <returns></returns>
        private SeriesData QueryDataFromDb(SQLiteConnection cnn, SeriesData seriesData, int minIndex = 0, int maxIndex = 0)
        {
            // Get the dataset column name
            string datasetColumnName = "EarthVelocityDS";
            switch (seriesData.Info.PlotType)
            {
                case PlotDataType.Magnitude:
                    datasetColumnName = "EarthVelocityDS";          // Velocity vectors from Earth Velocity
                    break;
                case PlotDataType.Direction:
                    datasetColumnName = "EarthVelocityDS";          // Velocity vectors from Earth Velocity
                    break;
                case PlotDataType.Amplitude:
                    datasetColumnName = "AmplitudeDS";              // Amplitude data
                    break;
                default:
                    datasetColumnName = "EarthVelocityDS";
                    break;
            }

            // Get the number of ensembles
            int numEnsembles = GetNumEnsembles(cnn, string.Format("SELECT COUNT(*) FROM tblEnsemble WHERE ({0} IS NOT NULL) {1} {2};",
                                                                    datasetColumnName,
                                                                    GenerateQueryFileList(),
                                                                    GenerateQuerySubsystemList()));
            // Update the progress bar
            StatusProgressMax = numEnsembles;

            // If min and max are used, set the limit and offset
            LimitOffset lo = CalcLimitOffset(numEnsembles, minIndex, maxIndex);
            numEnsembles = lo.Limit;

            // Update the progress bar
            StatusProgressMax = numEnsembles;

            // Get data
            string query = string.Format("SELECT ID,EnsembleNum,DateTime,EnsembleDS,AncillaryDS,BottomTrackDS,{0} FROM tblEnsemble WHERE ({1} IS NOT NULL) {2} {3} LIMIT {4} OFFSET {5};",
                                            datasetColumnName,
                                            datasetColumnName,
                                            GenerateQueryFileList(),
                                            GenerateQuerySubsystemList(),
                                            lo.Limit,
                                            lo.Offset);

            // Return the data to plot
            return GetDataFromDb(cnn, numEnsembles, query, seriesData);
        }

        /// <summary>
        /// Get the data from the project DB.
        /// </summary>
        /// <param name="cnn">Database connection.</param>
        /// <param name="numEnsembles">Number of ensembles.</param>
        /// <param name="query">Query string to retreive the data.</param>
        /// <param name="seriesData">Series Data and Info.</param>
        /// <returns>Magnitude data in (NumEns X NumBin) format.</returns>
        private SeriesData GetDataFromDb(SQLiteConnection cnn, int numEnsembles, string query, SeriesData seriesData)
        {
            // Init list
            int ensIndex = 0;

            // Ensure a connection was made
            if (cnn == null)
            {
                return null;
            }

            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = query;

                // Get Result
                DbDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // Convert the Earth JSON to an object
                    //Debug.WriteLine(reader["EnsembleNum"]);
                    // Update the status
                    StatusProgress++;
                    StatusMsg = reader["EnsembleNum"].ToString();

                    // Verify there is more data available
                    if (reader == null)
                    {
                        break;
                    }

                    // Ensure we do not exceed the number of ensembles
                    if (ensIndex >= numEnsembles)
                    {
                        break;
                    }

                    // Parse the data from the db
                    // This will be select which type of data to plot
                    double data = ParseData(reader, seriesData);

                    // If the array has not be created, created now
                    if (seriesData.Data == null)
                    {
                        // Create the array if this is the first entry
                        // NumEnsembles X NumBins
                        seriesData.Data = new List<DataPoint>();
                    }

                    seriesData.Data.Add(new DataPoint(ensIndex, data));

                    ensIndex++;
                }
            }

            return seriesData;
        }

        #endregion

        #region Parse Data

        /// <summary>
        /// Select which parser to use based off the selected plot.
        /// </summary>
        /// <param name="reader">Reader holds a single row (ensemble).</param>
        /// <param name="seriesData">Series Data and Info.</param>
        /// <returns>Data selected for the row.</returns>
        private double ParseData(DbDataReader reader, SeriesData seriesData)
        {
            switch (seriesData.Info.PlotType)
            {
                case PlotDataType.Magnitude:
                    return ParseMagData(reader, seriesData.Info.Bin);
                case PlotDataType.Direction:
                    return ParseDirData(reader, seriesData.Info.Bin);
                case PlotDataType.Amplitude:
                    return ParseAmpData(reader, seriesData.Info.Bin, seriesData.Info.Beam);
                default:
                    return 0.0;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <param name="bin">Bin number.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double ParseMagData(DbDataReader reader, int bin)
        {
            try
            {
                // Get the earth data as a JSON string
                string jsonEarth = reader["EarthVelocityDS"].ToString();

                if (!string.IsNullOrEmpty(jsonEarth))
                {
                    // Convert to a JSON object
                    JObject ensEarth = JObject.Parse(jsonEarth);

                    // Get the number of bins
                    int numBins = ensEarth["NumElements"].ToObject<int>();

                    // Get the velocity vector magntidue from the JSON object and add it to the array
                    double data = ensEarth["VelocityVectors"][bin]["Magnitude"].ToObject<double>();

                    return data;
                }

                return 0.0;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Earth Velocity Magnitude data row", e);
                return 0.0;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <param name="bin">Bin number.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double ParseDirData(DbDataReader reader, int bin)
        {
            try
            {
                // Get the earth data as a JSON string
                string jsonEarth = reader["EarthVelocityDS"].ToString();

                if (!string.IsNullOrEmpty(jsonEarth))
                {
                    // Convert to a JSON object
                    JObject ensEarth = JObject.Parse(jsonEarth);

                    // Get the number of bins
                    int numBins = ensEarth["NumElements"].ToObject<int>();

                    // Get the velocity vector magntidue from the JSON object and add it to the array
                    double data = ensEarth["VelocityVectors"][bin]["DirectionXNorth"].ToObject<double>();

                    return data;
                }

                return 0.0;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Earth Velocity Direction data row", e);
                return 0.0;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <param name="bin">Bin number.</param>
        /// <param name="beam">Beam number</param>
        /// <returns>Amplitude data for a row.</returns>
        private double ParseAmpData(DbDataReader reader, int bin, int beam)
        {
            try
            {
                // Get the earth data as a JSON string
                string json = reader["AmplitudeDS"].ToString();

                if (!string.IsNullOrEmpty(json))
                {
                    // Convert to a JSON object
                    JObject ens = JObject.Parse(json);

                    // Get the number of bins
                    int numBins = ens["NumElements"].ToObject<int>();

                    // Get the velocity vector magntidue from the JSON object and add it to the array
                    double data = ens["AmplitudeData"][bin][beam].ToObject<double>();

                    return data;
                }

                return 0.0;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Amplitude data row", e);
                return 0.0;
            }
        }

        #endregion

    }
}
