using GMap.NET;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsPresentation;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace PlotR
{
    class ShipTrackPlotViewModel : PlotViewModel
    {

        #region Class and Enum

        /// <summary>
        /// All the different map layers.
        /// </summary>
        public enum MapLayers
        {
            /// <summary>
            /// Ship Track line.
            /// </summary>
            Ship_Track_Line = 1,

            /// <summary>
            /// Quiver lines.
            /// </summary>
            Quiver = 2,

            /// <summary>
            /// Velocity Rectangles.
            /// </summary>
            Velocity_Rectangle = 3,

            /// <summary>
            /// Bottom Track Range Rectangles.
            /// </summary>
            Range_Rectangle = 4,

            /// <summary>
            /// Last point.
            /// </summary>
            Last_Point = 5,
        }

        /// <summary>
        /// Latitude and longitude position.
        /// </summary>
        public struct LatLon
        {
            /// <summary>
            /// Flag if the data is good.
            /// </summary>
            public bool IsGood { get; set; }

            /// <summary>
            /// Latitude in degrees.
            /// </summary>
            public double Latitude { get; set; }

            /// <summary>
            /// Longitude in degrees.
            /// </summary>
            public double Longitude { get; set; }
        }

        /// <summary>
        /// Store the ship track data.
        /// </summary>
        public class ShipTrackData
        {
            /// <summary>
            /// Date and time.
            /// </summary>
            public string DateTime { get; set; }

            /// <summary>
            /// Ensemble number.
            /// </summary>
            public string EnsNum { get; set; }

            /// <summary>
            /// Latitude and Longitude.
            /// </summary>
            public string LatLon { get; set; }

            /// <summary>
            /// Heading value.
            /// </summary>
            public double Heading { get; set; }

            /// <summary>
            /// Water Velocity magnitude and direction.
            /// </summary>
            public DbDataHelper.VelocityMagDir VelMagDir { get; set; }

            /// <summary>
            /// Average Range.
            /// </summary>
            public double AvgRange { get; set; }

            /// <summary>
            /// Initialize.
            /// </summary>
            public ShipTrackData()
            {
                DateTime = "";
                EnsNum = "";
                LatLon = "";
                VelMagDir = null;
                AvgRange = 0.0;
                Heading = 0.0;
            }

            /// <summary>
            /// Decode the latitude and longitude values.
            /// </summary>
            /// <returns></returns>
            public LatLon GetLatLon()
            {
                // Initialize
                LatLon latLon = new LatLon();
                latLon.Latitude = 0.0;
                latLon.Longitude = 0.0;

                if (!string.IsNullOrEmpty(LatLon))
                {
                    // Separate the position by comma
                    string[] lat_lon_items = LatLon.Split(',');

                    // Verify we found the lat and lon
                    if (lat_lon_items.Length >= 2)
                    {
                        // Parse the data
                        double lat = 0.0;
                        double lon = 0.0;
                        double.TryParse(lat_lon_items[0], out lat);
                        double.TryParse(lat_lon_items[1], out lon);

                        // Set the values
                        latLon.IsGood = true;
                        latLon.Latitude = lat;
                        latLon.Longitude = lon;
                     }
                }

                return latLon;
            }
        }

        #endregion

        #region Variables

        /// <summary>
        /// Minimum Index.
        /// </summary>
        private int _minIndex;

        /// <summary>
        /// Maximum index.
        /// </summary>
        private int _maxIndex;

        /// <summary>
        /// Quiver plot.
        /// </summary>
        public const string PLOT_OPTION_QUIVER = "Quiver";

        /// <summary>
        /// Velocity rectangle plot.
        /// </summary>
        public const string PLOT_OPTION_VELOCITY_RECTANGLE = "Velocity Rectangle";

        /// <summary>
        /// Bottom Track Range Rectangle.
        /// </summary>
        public const string PLOT_OPTION_BT_RANGE = "Bottom Track Range Rectangle";

        #endregion

        #region Properites

        #region Plot Scale

        /// <summary>
        /// Magnitude scale.  This value is mulitplied to the magnitude
        /// value to increase or decrease the line length for visual representation.
        /// </summary>
        private int _MagScale;
        /// <summary>
        /// Magnitude scale.  This value is mulitplied to the magnitude
        /// value to increase or decrease the line length for visual representation.
        /// </summary>
        public int MagScale
        {
            get { return _MagScale; }
            set
            {
                _MagScale = value;
                NotifyOfPropertyChange(() => MagScale);

                ReplotData();
            }
        }

        /// <summary>
        /// Set the minimum value.
        /// </summary>
        private double _MinValue;
        /// <summary>
        /// Set the minimum value.
        /// </summary>
        public double MinValue
        {
            get { return _MinValue; }
            set
            {
                _MinValue = value;
                NotifyOfPropertyChange(() => MinValue);

                if(_MinValue > _MaxValue)
                {
                    MaxValue += 1;
                }

                // Replot the data
                ReplotData();

                // Set the color map canvas
                ColorMapCanvas = ColorHM.GetColorMapLegend(_MinValue, _MaxValue);
                NotifyOfPropertyChange(() => ColorMapCanvas);
            }
        }

        /// <summary>
        /// Set the maximum value.
        /// </summary>
        private double _MaxValue;
        /// <summary>
        /// Set the maximum value.
        /// </summary>
        public double MaxValue
        {
            get { return _MaxValue; }
            set
            {
                _MaxValue = value;
                NotifyOfPropertyChange(() => MaxValue);

                if(_MaxValue < _MinValue)
                {
                    MinValue -= 1;
                }

                // Replot the data
                ReplotData();

                // Set the color map canvas
                ColorMapCanvas = ColorHM.GetColorMapLegend(_MinValue, _MaxValue);
                NotifyOfPropertyChange(() => ColorMapCanvas);
            }
        }

        #endregion

        #region GMap

        /// <summary>
        /// List of all the markers.
        /// </summary>
        public ObservableCollection<GMapMarker> Markers { get; set; }

        /// <summary>
        /// Current position on the map.
        /// </summary>
        private GMap.NET.PointLatLng _Position;
        /// <summary>
        /// Current position on the map.
        /// </summary>
        public GMap.NET.PointLatLng Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                NotifyOfPropertyChange(() => Position);
            }
        }

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        private int _Zoom;
        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        public int Zoom
        {
            get { return _Zoom; }
            set
            {
                _Zoom = value;
                NotifyOfPropertyChange(() => Zoom);
            }
        }

        /// <summary>
        /// List of map providers.
        /// </summary>
        public ObservableCollection<GMap.NET.MapProviders.GMapProvider> MapProviderList { get; set; }

        /// <summary>
        /// Map Provider.
        /// </summary>
        private GMap.NET.MapProviders.GMapProvider _SelectedMapProvider;
        /// <summary>
        /// Map provider.
        /// </summary>
        public GMap.NET.MapProviders.GMapProvider SelectedMapProvider
        {
            get
            {
                return _SelectedMapProvider;
            }
            set
            {
                _SelectedMapProvider = value;
                NotifyOfPropertyChange(() => SelectedMapProvider);

                ((ShipTrackPlotView)this.GetView()).MapView.ReloadMap();
            }
        }

        /// <summary>
        /// Zoom type.
        /// </summary>
        public GMap.NET.MouseWheelZoomType MouseWheelZoomType { get; set; }


        /// <summary>
        /// Convert the values to a color based off a color map.
        /// </summary>
        public ColorHeatMap ColorHM { get; set; }

        /// <summary>
        /// Color map canvas to display the options.
        /// </summary>
        public System.Windows.Controls.Canvas ColorMapCanvas { get; set; }

        #endregion

        #region Plot Options

        /// <summary>
        /// Flag to mark bad below bottom.
        /// </summary>
        private bool _IsMarkBadBelowBottom;
        /// <summary>
        /// Flag to mark bad below bottom.
        /// </summary>
        public bool IsMarkBadBelowBottom
        {
            get { return _IsMarkBadBelowBottom; }
            set
            {
                _IsMarkBadBelowBottom = value;
                NotifyOfPropertyChange(() => IsMarkBadBelowBottom);

                ReplotData();
            }
        }

        /// <summary>
        /// Plot a water line or rectangle.
        /// </summary>
        private bool _IsPlotWaterLine;
        /// <summary>
        /// Plot a water line or rectangle.
        /// </summary>
        public bool IsPlotWaterLine
        {
            get { return _IsPlotWaterLine; }
            set
            {
                _IsPlotWaterLine = value;
                NotifyOfPropertyChange(() => IsPlotWaterLine);

                // Reset the mag scale
                if(value)
                {
                    // Use default magScale
                    _MagScale = 75;
                    NotifyOfPropertyChange(() => MagScale);
                }
                else
                {
                    _MagScale = 1;
                    NotifyOfPropertyChange(() => MagScale);
                }

                ReplotData();
            }
        }

        /// <summary>
        /// List of Plot options.
        /// </summary>
        public ObservableCollection<string> PlotOptionList { get; set; }

        /// <summary>
        /// Selected plot option.
        /// </summary>
        private string _SelectedPlotOption;
        /// <summary>
        /// Selected plot option.
        /// </summary>
        public string SelectedPlotOption
        {
            get { return _SelectedPlotOption; }
            set
            {
                _SelectedPlotOption = value;
                NotifyOfPropertyChange(() => SelectedPlotOption);

                // Set the plot option
                SetPlotOption(value);
            }
        }

        /// <summary>
        /// Use GPS speed as a backup value.
        /// </summary>
        private bool _IsUseGpsSpeedBackup;
        /// <summary>
        /// Use GPS speed as a backup value.
        /// </summary>
        public bool IsUseGpsSpeedBackup
        {
            get { return _IsUseGpsSpeedBackup; }
            set
            {
                _IsUseGpsSpeedBackup = value;
                NotifyOfPropertyChange(() => IsUseGpsSpeedBackup);

                ReplotData();
            }
        }




        #endregion

        #region Last Marker

        /// <summary>
        /// Is the last marker an ellipse or triangle.
        /// </summary>
        private bool _IsLastMarkerEllipse;
        /// <summary>
        /// Is the last marker an ellipse or triangle.
        /// </summary>
        public bool IsLastMarkerEllipse
        {
            get { return _IsLastMarkerEllipse; }
            set
            {
                _IsLastMarkerEllipse = value;
                NotifyOfPropertyChange(() => IsLastMarkerEllipse);

                ReplotData();
            }
        }

        /// <summary>
        /// Last marker scale.
        /// </summary>
        private double _LastMarkerScale;
        /// <summary>
        /// Last marker scale.
        /// </summary>
        public double LastMarkerScale
        {
            get { return _LastMarkerScale; }
            set
            {
                _LastMarkerScale = value;
                NotifyOfPropertyChange(() => LastMarkerScale);

                ReplotData();
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Add a series to the plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> TakeScreenShotCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initialize the map.
        /// </summary>
        public ShipTrackPlotViewModel()
        {
            // Create the plot
            //Plot = CreatePlot();

            ColorHM = new ColorHeatMap(0x80);      // 50% alpha

            // Create GMap
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;

            MapProviderList = new ObservableCollection<GMap.NET.MapProviders.GMapProvider>();
            MapProviderList.Add(GMap.NET.MapProviders.GoogleMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.GoogleHybridMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.BingMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.OpenStreetMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.GoogleChinaMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.ArcGIS_World_Topo_MapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.ArcGIS_World_Physical_MapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.ArcGIS_World_Terrain_Base_MapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.HereHybridMapProvider.Instance);
            MapProviderList.Add(GMap.NET.MapProviders.LithuaniaMapProvider.Instance);
            _SelectedMapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            NotifyOfPropertyChange(() => SelectedMapProvider);

            PlotOptionList = new ObservableCollection<string>();
            PlotOptionList.Add(PLOT_OPTION_QUIVER);
            PlotOptionList.Add(PLOT_OPTION_VELOCITY_RECTANGLE);
            PlotOptionList.Add(PLOT_OPTION_BT_RANGE);
            _SelectedPlotOption = PLOT_OPTION_QUIVER;
            NotifyOfPropertyChange(() => SelectedPlotOption);


            _minIndex = 0;
            _maxIndex = 0;
            Markers = new ObservableCollection<GMapMarker>();
            Position = new PointLatLng();
            Zoom = 1;
            _IsPlotWaterLine = true;
            _IsMarkBadBelowBottom = true;
            _IsUseGpsSpeedBackup = true;
            _IsLastMarkerEllipse = false;
            _MagScale = 75;
            _MinValue = 0.0;
            _MaxValue = 2.0;
            _LastMarkerScale = 10.0;
            NotifyOfPropertyChange(() => MagScale);
            NotifyOfPropertyChange(() => MinValue);
            NotifyOfPropertyChange(() => MaxValue);
            NotifyOfPropertyChange(() => IsPlotWaterLine);
            NotifyOfPropertyChange(() => IsMarkBadBelowBottom);
            NotifyOfPropertyChange(() => IsUseGpsSpeedBackup);
            NotifyOfPropertyChange(() => LastMarkerScale);
            NotifyOfPropertyChange(() => IsLastMarkerEllipse);

            ColorMapCanvas = ColorHM.GetColorMapLegend(_MinValue, _MaxValue);       // Set the color map canvas

            // To force shutdown of the GMAP
            //MapView.Manager.CancelTileCaching();

            // Take a Screen Shot
            this.TakeScreenShotCommand = ReactiveCommand.Create(() => TakeScreenShot());
            
        }

        #region Create Plot

        /// <summary>
        /// Create the plot.
        /// </summary>
        /// <returns></returns>
        private ViewResolvingPlotModel CreatePlot()
        {
            ViewResolvingPlotModel temp = new ViewResolvingPlotModel();

            temp.IsLegendVisible = true;

            //temp.Background = OxyColors.Black;
            //temp.TextColor = OxyColors.White;
            //temp.PlotAreaBorderColor = OxyColors.White;

            temp.Title = "Distance Made Good";

            // Setup the axis
            //var c = OxyColors.White;
            temp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                //Minimum = 0,
                //StartPosition = 1,                                              // This will invert the axis to start at the top with minimum value
                //EndPosition = 0
                //TicklineColor = OxyColors.White,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c),
                //IntervalLength = 5,
                MinimumPadding = 0.1,                                               // Pad the top and bottom of the plot so min/max lines can be seen
                MaximumPadding = 0.1,                                               // Pad the top and bottom of the plot so min/max lines can be seen
                Unit = "latitude"
            });
            temp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                //MajorStep = 1
                //Minimum = 0,
                //Maximum = _maxDataSets,
                //TicklineColor = OxyColors.White,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c),
                //IntervalLength = 5,
                //TickStyle = OxyPlot.Axes.TickStyle.None,
                //IsAxisVisible = false,
                Unit = "longitude"
            });

            temp.Series.Add(new LineSeries() { Color = OxyColors.Chartreuse, StrokeThickness = 1, Title = "Ship Track" });

            return temp;
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

        #region Replot Data

        /// <summary>
        /// Implement reploting the data.
        /// </summary>
        /// <param name="minIndex">Minimum Index.</param>
        /// <param name="maxIndex">Maximum Index.</param>
        public override void ReplotData(int minIndex, int maxIndex)
        {
            //Application.Current.Dispatcher.Invoke((Action)delegate
            //{
            Task.Run(() =>
            { 
                _minIndex = minIndex;
                _maxIndex = maxIndex;
                DrawPlot(minIndex, maxIndex);

            });
        }

        /// <summary>
        /// Implement replotting the data.
        /// </summary>
        public override void ReplotData()
        {
            // Replot the data
            ReplotData(_minIndex, _maxIndex);
        }

        #endregion

        #region Draw Plot

        /// <summary>
        /// Get the data from the database.  Then draw the plot.
        /// </summary>
        /// <param name="minIndex">Minimum index in the database.</param>
        /// <param name="maxIndex">Maximum index in the database.</param>
        private async void DrawPlot(int minIndex, int maxIndex)
        {
            // Clear the current markers
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Markers.Clear();
            });

            // Init the list of data
            List<ShipTrackData> data = new List<ShipTrackData>();

            // Verify a file was given
            if (!string.IsNullOrEmpty(_ProjectFilePath))
            {
                // Verify the file exist
                if (File.Exists(_ProjectFilePath))
                {
                    // Create data Source string
                    string dataSource = string.Format("Data Source={0};Version=3;", _ProjectFilePath);

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

                            // Get the data from the project
                            await Task.Run(() => data = GetData(sqlite_conn, _MagScale, minIndex, maxIndex));

                            // Close connection
                            sqlite_conn.Close();
                        }

                        // If there is no data, do not plot
                        if (data != null && data.Count > 0)
                        {
                            // Update status
                            StatusMsg = "Drawing Plot";

                            Application.Current.Dispatcher.Invoke((Action)delegate
                            {
                                // Plot the data
                                PlotMapData(data);
                            });
                        }
                        else
                        {
                            StatusMsg = "No GPS data to plot";
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
                }
            }
        }

        #endregion

        #region Get Data

        /// <summary>
        /// Get the data based off the selected data type.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="magScale">Magnitude scale.</param>
        /// <param name="minIndex">Minimum Ensemble index.</param>
        /// <param name="maxIndex">Maximum Ensemble index.</param>
        /// <returns>The selected for each ensemble and bin.</returns>
        private List<ShipTrackData> GetData(SQLiteConnection cnn, double magScale, int minIndex = 0, int maxIndex = 0)
        {
            //StatusProgressMax = TotalNumEnsembles;
            StatusProgress = 0;

            string datasetColumnName = "Position";

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

            // Get data
            string query = string.Format("SELECT ID,EnsembleNum,DateTime,EnsembleDS,AncillaryDS,BottomTrackDS,EarthVelocityDS,NmeaDS,{0} FROM tblEnsemble WHERE ({1} IS NOT NULL) {2} {3} LIMIT {4} OFFSET {5};",
                                            datasetColumnName,
                                            datasetColumnName,
                                            GenerateQueryFileList(),
                                            GenerateQuerySubsystemList(),
                                            lo.Limit,
                                            lo.Offset);

            // Get the data to plot
            return QueryDataFromDb(cnn, query, magScale, minIndex, maxIndex);
        }

        /// <summary>
        /// Query the data from the database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="query">Query for the data.</param>
        /// <param name="magScale">Magnitude scale.</param>
        /// <param name="minIndex">Minimum index.</param>
        /// <param name="maxIndex">Maximum index.</param>
        /// <returns></returns>
        private List<ShipTrackData> QueryDataFromDb(SQLiteConnection cnn, string query, double magScale, int minIndex = 0, int maxIndex = 0)
        {
            // Init list
            double backupBtEast = DbDataHelper.BAD_VELOCITY;
            double backupBtNorth = DbDataHelper.BAD_VELOCITY;

            // Init the new series data
            List<ShipTrackData> stDataList = new List<ShipTrackData>();
            //stData.MagScale = magScale;

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
                    ShipTrackData stData = new ShipTrackData();

                    // Update the status
                    StatusProgress++;
                    StatusMsg = reader["EnsembleNum"].ToString();

                    // Get the Ensemble number and Date and time
                    stData.DateTime = reader["DateTime"].ToString();
                    stData.EnsNum = reader["EnsembleNum"].ToString();

                    // Plot the lat/lon
                    stData.LatLon = reader["Position"].ToString();

                    // Heading
                    DbDataHelper.HPR hpr = DbDataHelper.GetHPR(reader);
                    stData.Heading = hpr.Heading;

                    //// Get the range bin
                    //int rangeBin = DbDataHelper.GetRangeBin(reader);

                    //// Get the magnitude data
                    //string jsonEarth = reader["EarthVelocityDS"].ToString();
                    //if (!string.IsNullOrEmpty(jsonEarth))
                    //{
                    //    // Convert to a JSON object
                    //    JObject ensEarth = JObject.Parse(jsonEarth);

                    //    // Average the data
                    //    avgMag = DbDataHelper.GetAvgMag(ensEarth, IsMarkBadBelowBottom, rangeBin, DbDataHelper.BAD_VELOCITY);
                    //    avgDir = DbDataHelper.GetAvgDir(ensEarth, IsMarkBadBelowBottom, rangeBin, DbDataHelper.BAD_VELOCITY);

                    //    //Debug.WriteLine(string.Format("Avg Dir: {0} Avg Mag: {1}", avgDir, avgMag));
                    //}

                    if (IsUseGpsSpeedBackup)
                    {
                        // Get the GPS data from the database
                        DbDataHelper.GpsData gpsData = DbDataHelper.GetGpsData(reader);

                        // Check for a backup value for BT East and North speed from the GPS if a Bottom Track value is never found
                        if (Math.Round(backupBtEast, 4) == BAD_VELOCITY && gpsData.IsBackShipSpeedGood)
                        {
                            backupBtEast = gpsData.BackupShipEast;
                            backupBtNorth = gpsData.BackupShipNorth;
                        }
                    }

                    // Get the velocity
                    stData.VelMagDir = DbDataHelper.CreateVelocityVectors(reader, backupBtEast, backupBtNorth, true, true);

                    // Get the average range
                    stData.AvgRange = DbDataHelper.GetAverageRange(reader);

                    // Store the backup value
                    if (stData.VelMagDir.IsBtVelGood)
                    {
                        backupBtEast = stData.VelMagDir.BtEastVel;
                        backupBtNorth = stData.VelMagDir.BtNorthVel;
                    }

                    // Add the data to the list
                    stDataList.Add(stData);
                }
            }

            return stDataList;
        }

        /// <summary>
        /// Find the center point.
        /// </summary>
        /// <param name="markers">List of markers.</param>
        /// <returns>Center point.</returns>
        private PointLatLng GetCenterPoint(ObservableCollection<GMapMarker> markers)
        {
            PointLatLng centroide = new PointLatLng();
            int sum = 0;
            double lat = 0.0;
            double lng = 0.0;
            foreach (GMapMarker pts in markers)
            {
                sum++;
                lat += pts.Position.Lat;
                lng += pts.Position.Lng;

            }
            if (sum > 0)
            {
                lat = lat / sum;
                lng = lng / sum;
            }

            centroide.Lat = lat;
            centroide.Lng = lng;

            return centroide;
        }

        #endregion

        #region Plot Series

        /// <summary>
        /// Plot the series.  This will remove all the old series.  Then add the 
        /// new series lines.
        /// </summary>
        /// <param name="stData">Ship Track data.</param>
        private void PlotMapData(List<ShipTrackData> stDataList)
        {
            // Init the value
            double avgMag = 0.0;
            double avgDir = 0.0;

            // Keep Track of previous point to draw
            // the ship track line
            //LatLon prevLatLon = new LatLon { Latitude = 0.0, Longitude = 0.0, IsGood = false };
            IList<PointLatLng> points = new List<PointLatLng>();

            // Last point
            ShipTrackData lastGoodShipTrack = null;

            foreach (ShipTrackData stData in stDataList)
            {
                avgMag = stData.VelMagDir.AvgMagnitude;
                avgDir = stData.VelMagDir.AvgDirectionYNorth;
                LatLon currLatLon = stData.GetLatLon();

                if (currLatLon.IsGood)
                {
                    // Add the point to the route
                    points.Add(new PointLatLng(currLatLon.Latitude, currLatLon.Longitude));

                    // Convert the value to color from the color map
                    System.Windows.Media.SolidColorBrush brush = new System.Windows.Media.SolidColorBrush(ColorHM.GetColorForValue(avgMag, _MinValue, _MaxValue));

                    // Mark
                    GMapMarker marker = new GMapMarker(new GMap.NET.PointLatLng(currLatLon.Latitude, currLatLon.Longitude));
                    //System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();

                    if (_SelectedPlotOption == PLOT_OPTION_QUIVER)
                    {
                        // Degrees to radian
                        double angle = Math.PI * avgDir / 180.0;

                        marker.Shape = new Line
                        {
                            X1 = 0,
                            Y1 = 0,
                            X2 = (Math.Abs(avgMag) * MagScale) * Math.Cos(angle),
                            Y2 = -((Math.Abs(avgMag) * MagScale) * Math.Sin(angle)),        // Flip the sign
                            StrokeThickness = 3,
                            Stroke = brush,
                            ToolTip = string.Format("[{0}] [{1}] Mag: {2} Dir: {3} Range: {4} Heading: {5}", stData.EnsNum, stData.DateTime, avgMag.ToString("0.0"), avgDir.ToString("0.0"), stData.AvgRange.ToString("0.0"), stData.Heading.ToString("0.0")),
                            Name = string.Format("Line_{0}", stData.EnsNum)
                        };
                        marker.ZIndex = (int)MapLayers.Quiver;
                    }
                    else if(_SelectedPlotOption == PLOT_OPTION_VELOCITY_RECTANGLE)
                    {
                        marker.Shape = new Rectangle
                        {
                            Width = 20 * MagScale,
                            Height = 20 * MagScale,
                            Fill = brush,
                            Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                            ToolTip = string.Format("[{0}] [{1}] Mag: {2} Dir: {3} Range: {4} Heading: {5}", stData.EnsNum, stData.DateTime, avgMag.ToString("0.0"), avgDir.ToString("0.0"), stData.AvgRange.ToString("0.0"), stData.Heading.ToString("0.0")),
                            Name = string.Format("Rect_Vel_{0}", stData.EnsNum)
                        };
                        marker.ZIndex = (int)MapLayers.Velocity_Rectangle;
                    }
                    else if(_SelectedPlotOption == PLOT_OPTION_BT_RANGE)
                    {
                        // Convert the average Range to color from the color map
                        brush = new System.Windows.Media.SolidColorBrush(ColorHM.GetColorForValue(stData.AvgRange, _MinValue, _MaxValue));

                        marker.Shape = new Rectangle
                        {
                            Width = 20 * MagScale,
                            Height = 20 * MagScale,
                            Fill = brush,
                            Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                            ToolTip = string.Format("[{0}] [{1}] Mag: {2} Dir: {3} Range: {4} Heading: {5}", stData.EnsNum, stData.DateTime, avgMag.ToString("0.0"), avgDir.ToString("0.0"), stData.AvgRange.ToString("0.0"), stData.Heading.ToString("0.0")),
                            Name = string.Format("Rect_Range_{0}", stData.EnsNum)
                        };
                        marker.ZIndex = (int)MapLayers.Range_Rectangle;
                    }

                    // Record the last point to get a special marker
                    lastGoodShipTrack = stData;

                    // Add the marker to the list
                    Markers.Add(marker);
                }
            }



            // Plot the path that we taken
            GMapRoute route = new GMapRoute(points);
            System.Windows.Media.SolidColorBrush routeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            routeBrush.Opacity = 0.4;
            route.Shape = new System.Windows.Shapes.Path() { StrokeThickness = 3, Stroke = routeBrush, Fill = routeBrush };
            route.ZIndex = (int)MapLayers.Ship_Track_Line;
            Markers.Add(route);

            // Add a marker for the last point to know which direction it traveled
            if (lastGoodShipTrack != null)
            {
                LatLon lastLatLon = lastGoodShipTrack.GetLatLon();

                if (lastLatLon.IsGood)
                {
                    GMapMarker lastMarker = new GMapMarker(new GMap.NET.PointLatLng(lastLatLon.Latitude, lastLatLon.Longitude));
                    if (IsLastMarkerEllipse)
                    {
                        lastMarker.Shape = new Ellipse
                        {
                            //Points = trianglePts,
                            RenderTransform = new System.Windows.Media.TranslateTransform(-((1 * LastMarkerScale) / 2), -((1 * LastMarkerScale) / 2)),
                            Width = 1 * LastMarkerScale,
                            Height = 1 * LastMarkerScale,
                            Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow),
                            Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                            ToolTip = string.Format("[{0}] [{1}] Mag: {2} Dir: {3} Range: {4} Heading: {5}", lastGoodShipTrack.EnsNum, lastGoodShipTrack.DateTime, avgMag.ToString("0.0"), avgDir.ToString("0.0"), lastGoodShipTrack.AvgRange.ToString("0.0"), lastGoodShipTrack.Heading.ToString("0.0"))
                        };
                    }
                    else
                    {
                        System.Windows.Media.PointCollection trianglePts = new System.Windows.Media.PointCollection();
                        //trianglePts.Add(new Point(0.0, 1.0 * LastMarkerScale));
                        //trianglePts.Add(new Point(0.0, -1.0 * LastMarkerScale));
                        //trianglePts.Add(new Point(-1.0 * LastMarkerScale, 0.0));
                        trianglePts.Add(new Point(-1.0 * LastMarkerScale, 0.0));
                        trianglePts.Add(new Point(1.0 * LastMarkerScale, 0.0));
                        trianglePts.Add(new Point(0.0, -1.0 * LastMarkerScale));
                        lastMarker.Shape = new Polygon
                        {
                            Points = trianglePts,
                            RenderTransform = new System.Windows.Media.RotateTransform(lastGoodShipTrack.Heading),
                            Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow),
                            Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                            ToolTip = string.Format("[{0}] [{1}] Mag: {2} Dir: {3} Range: {4} Heading: {5}", lastGoodShipTrack.EnsNum, lastGoodShipTrack.DateTime, avgMag.ToString("0.0"), avgDir.ToString("0.0"), lastGoodShipTrack.AvgRange.ToString("0.0"), lastGoodShipTrack.Heading.ToString("0.0"))
                        };
                    }

                    // Set the zIndex so we can filter the layers
                    lastMarker.ZIndex = (int)MapLayers.Last_Point;

                    // Add the last point to the end
                    Markers.Add(lastMarker);
                }
            }

            // Set the center position
            Position = GetCenterPoint(Markers);
            Zoom = 17;
        }



        #endregion

        #region Take Screen Shot

        public void TakeScreenShot()
        {
            ((ShipTrackPlotView)this.GetView()).TakeScreenShot();
        }

        #endregion

        #region Plot Option

        /// <summary>
        /// Set the plot options.
        /// </summary>
        /// <param name="option"></param>
        public void SetPlotOption(string option)
        {
            // Reset the mag scale
            switch (option)
            {
                case PLOT_OPTION_QUIVER:
                    // Use default magScale
                    _MagScale = 75;
                    NotifyOfPropertyChange(() => MagScale);
                    _MinValue = 0;
                    _MaxValue = 2;
                    NotifyOfPropertyChange(() => MinValue);
                    NotifyOfPropertyChange(() => MaxValue);
                    break;
                case PLOT_OPTION_VELOCITY_RECTANGLE:
                    _MagScale = 1;
                    NotifyOfPropertyChange(() => MagScale);
                    _MinValue = 0;
                    _MaxValue = 2;
                    NotifyOfPropertyChange(() => MinValue);
                    NotifyOfPropertyChange(() => MaxValue);
                    break;
                case PLOT_OPTION_BT_RANGE:
                    _MagScale = 1;
                    NotifyOfPropertyChange(() => MagScale);
                    _MinValue = 0;
                    _MaxValue = 100;
                    NotifyOfPropertyChange(() => MinValue);
                    NotifyOfPropertyChange(() => MaxValue);
                    break;
                    
            }

            // Update the color map
            //ColorMapCanvas = ColorHM.GetColorMapLegend(_MinValue, _MaxValue);
            NotifyOfPropertyChange(() => ColorMapCanvas);

            // Replot the data
            ReplotData();
        }

        #endregion

    }
}
