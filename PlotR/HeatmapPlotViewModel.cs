/*
 * Copyright © 2011 
 * Rowe Technology Inc.
 * All rights reserved.
 * http://www.rowetechinc.com
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification is NOT permitted.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
 * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 04/25/2018      RC          1.0.0       Initial coding    
 * 
 */

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
    /// <summary>
    /// Heat map plot.
    /// </summary>
    public class HeatmapPlotViewModel : PlotViewModel
    {
        #region Class

        /// <summary>
        /// Class to hold the plot data.
        /// </summary>
        private class PlotData
        {
            /// <summary>
            /// Profile data to plot.
            /// </summary>
            public double[,] ProfileData { get; set; }

            /// <summary>
            /// Bottom Track data to plot.
            /// </summary>
            public AreaSeries BottomTrackData { get; set; }

            /// <summary>
            /// Initialize the object.
            /// </summary>
            public PlotData()
            {
                // Store the Profile data
                ProfileData = null;

                // Create a Bottom Track series
                BottomTrackData = null;
            }
        }

        #endregion

        #region Variables

        /// <summary>
        /// Color legend key.
        /// </summary>
        private string COLOR_LEGEND_KEY = "ColorLegend";

        /// <summary>
        /// Store the last good Bottom Track Range Bin value.
        /// This is used as a backup value for Bottom Track Range Bin value if
        /// the current bottom track is bad.
        /// </summary>
        private int _prevGoodBottomBin;

        /// <summary>
        /// Axis to show the depth.
        /// </summary>
        LinearAxis _depthAxis;

        /// <summary>
        /// Axis to show the bins.
        /// </summary>
        LinearAxis _binAxis;

        /// <summary>
        /// Backup Bottom Track East value.
        /// </summary>
        double _backupBtEast;

        /// <summary>
        /// Backup Bottom Track North Value.
        /// </summary>
        double _backupBtNorth;

        #endregion

        #region Properties

        #region Plot Types

        /// <summary>
        /// Selected Plot type.
        /// </summary>
        protected PlotDataType _SelectedPlotType;
        /// <summary>
        /// Selected Plot type.
        /// </summary>
        public PlotDataType SelectedPlotType
        {
            get { return _SelectedPlotType; }
            set
            {
                _SelectedPlotType = value;
                NotifyOfPropertyChange(() => SelectedPlotType);

                // Replot data
                ReplotData(_SelectedPlotType);
            }
        }

        /// <summary>
        /// Magnitude Plot Selected.
        /// </summary>
        protected bool _IsMagnitude;
        /// <summary>
        /// Magnitude Plot Selected.
        /// </summary>
        public bool IsMagnitude
        {
            get { return _IsMagnitude; }
            set
            {
                _IsMagnitude = value;
                NotifyOfPropertyChange(() => IsMagnitude);

                if (value)
                {
                    // Replot data
                    ReplotData(PlotDataType.Magnitude);
                }
            }
        }

        /// <summary>
        /// Water Direction Plot Selected.
        /// </summary>
        protected bool _IsDirection;
        /// <summary>
        /// Water Direction Plot Selected.
        /// </summary>
        public bool IsDirection
        {
            get { return _IsDirection; }
            set
            {
                _IsDirection = value;
                NotifyOfPropertyChange(() => IsDirection);

                if (value)
                {
                    // Replot data
                    ReplotData(PlotDataType.Direction);
                }
            }
        }

        /// <summary>
        /// Amplitude Plot Selected.
        /// </summary>
        protected bool _IsAmplitude;
        /// <summary>
        /// Amplitude Plot Selected.
        /// </summary>
        public bool IsAmplitude
        {
            get { return _IsAmplitude; }
            set
            {
                _IsAmplitude = value;
                NotifyOfPropertyChange(() => IsAmplitude);

                if (value)
                {
                    // Replot data
                    ReplotData(PlotDataType.Amplitude);
                }
            }
        }

        #endregion

        #region Bottom Track Line

        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        private bool _IsBottomTrackLine;
        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        public bool IsBottomTrackLine
        {
            get { return _IsBottomTrackLine; }
            set
            {
                _IsBottomTrackLine = value;
                NotifyOfPropertyChange(() => IsBottomTrackLine);
                
                // Replot data
                ReplotData(SelectedPlotType);
            }
        }

        #endregion

        #region Mark Bad Below Bottom

        /// <summary>
        /// Select to Mark Bad Below Bottom.
        /// </summary>
        private bool _IsMarkBadBelowBottom;
        /// <summary>
        /// Select to Mark Bad Below Bottom.
        /// </summary>
        public bool IsMarkBadBelowBottom
        {
            get { return _IsMarkBadBelowBottom; }
            set
            {
                _IsMarkBadBelowBottom = value;
                NotifyOfPropertyChange(() => IsMarkBadBelowBottom);

                // Replot data
                ReplotData(SelectedPlotType);
            }
        }

        #endregion

        /// <summary>
        /// Remove the ship speed.
        /// </summary>
        private bool _IsRemoveShipSpeed;
        /// <summary>
        /// Remove the ship speed.
        /// </summary>
        public bool IsRemoveShipSpeed
        {
            get { return _IsRemoveShipSpeed; }
            set
            {
                _IsRemoveShipSpeed = value;
                NotifyOfPropertyChange(() => IsRemoveShipSpeed);

                // Replot data
                ReplotData(SelectedPlotType);
            }
        }

        #region Bottom Track Line

        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        private bool _IsInterpolate;
        /// <summary>
        /// Bottom Track Line selection.
        /// </summary>
        public bool IsInterpolate
        {
            get { return _IsInterpolate; }
            set
            {
                _IsInterpolate = value;
                NotifyOfPropertyChange(() => IsInterpolate);

                // Replot data
                // Lock the plot for an update
                lock (Plot.SyncRoot)
                {
                    foreach (var series in Plot.Series)
                    {
                        if (series.GetType() == typeof(HeatMapSeries))
                        {
                            ((HeatMapSeries)series).Interpolate = value;
                        }
                    }
                }

                // Then refresh the plot
                Plot.InvalidatePlot(true);
            }
        }

        #endregion

        #region Plot Color Selection

        public BindingList<OxyPalette> PaletteList { get; set; }

        /// <summary>
        /// Selected palette.
        /// </summary>
        private OxyPalette _SelectedPalette;
        /// <summary>
        /// Selected palette.
        /// </summary>
        public OxyPalette SelectedPalette
        {
            get { return _SelectedPalette; }
            set
            {
                _SelectedPalette = value;
                NotifyOfPropertyChange(() => SelectedPalette);

                // Set the new plot palette
                foreach(var axis in Plot.Axes)
                {
                    if(axis.Key == COLOR_LEGEND_KEY)
                    {
                        // Change the palette
                        ((LinearColorAxis)axis).Palette = SelectedPalette;

                        // Update the plot
                        Plot.InvalidatePlot(true);
                    }
                }
            }
        }

        #endregion

        #region Min and Max Legend 

        /// <summary>
        /// Current Minimum value.
        /// </summary>
        private double _CurrentMinValue;
        /// <summary>
        /// Current Minimum value.
        /// </summary>
        public double CurrentMinValue
        {
            get { return _CurrentMinValue; }
            set
            {
                _CurrentMinValue = value;
                NotifyOfPropertyChange(() => CurrentMinValue);

                // Set the new plot palette
                SetMinMaxColorAxis(_CurrentMinValue, _CurrentMaxValue);
            }
        }

        /// <summary>
        /// Current Maximum value.
        /// </summary>
        private double _CurrentMaxValue;
        /// <summary>
        /// Current Maximum value.
        /// </summary>
        public double CurrentMaxValue
        {
            get { return _CurrentMaxValue; }
            set
            {
                _CurrentMaxValue = value;
                NotifyOfPropertyChange(() => CurrentMaxValue);

                // Set the new plot palette
                SetMinMaxColorAxis(_CurrentMinValue, _CurrentMaxValue);
            }
        }


        #endregion

        #endregion


        /// <summary>
        /// Initialize the plot.
        /// </summary>
        public HeatmapPlotViewModel()
            : base()
        {
            // Initialize
            ProjectFilePath = "";
            Plot = CreatePlot();

            // Selected Plot Type
            //PlotTypeList = Enum.GetValues(typeof(PlotDataType)).Cast<PlotDataType>().ToList();
            SelectedPlotType = PlotDataType.Magnitude;
            IsMagnitude = true;
            _IsInterpolate = false;

            // Create Palette
            CreatePaletteList();
            _SelectedPalette = OxyPalettes.Jet(64);

            // Initialize
            CurrentMinValue = 0.0;
            CurrentMaxValue = 2.0;
            _backupBtEast = DbDataHelper.BAD_VELOCITY;
            _backupBtNorth = DbDataHelper.BAD_VELOCITY;

            // Bottom Track Line
            _IsBottomTrackLine = true;
            NotifyOfPropertyChange(() => IsBottomTrackLine);

            // Mark Bad Below Bottom
            _IsMarkBadBelowBottom = true;
            NotifyOfPropertyChange(() => IsMarkBadBelowBottom);
            _prevGoodBottomBin = BAD_BOTTOM_BIN;

            // Remove Ship Speed
            _IsRemoveShipSpeed = true;
            NotifyOfPropertyChange(() => IsRemoveShipSpeed);

        }

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

            // Selected Plot Type
            _SelectedPlotType = PlotDataType.Magnitude;
            NotifyOfPropertyChange(() => SelectedPlotType);
            _IsMagnitude = true;
            NotifyOfPropertyChange(() => IsMagnitude);

            // Draw the plot
            DrawPlot(fileName, _SelectedPlotType, minIndex, maxIndex);
        }

        #endregion

        #region Get Data

        /// <summary>
        /// Get the data based off the selected data type.
        /// </summary>
        /// <param name="ePlotDataType">Selected data type.</param>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="maxNumEnsembles">Max number of ensembles to display.</param>
        /// <param name="selectedPlotType">Selected Plot type.</param>
        /// <returns>The selected for each ensemble and bin.</returns>
        private PlotData GetData(SQLiteConnection cnn, int maxNumEnsembles, PlotDataType selectedPlotType, int minIndex = 0, int maxIndex = 0)
        {
            //StatusProgressMax = TotalNumEnsembles;
            StatusProgress = 0;
            _backupBtEast = DbDataHelper.BAD_VELOCITY;
            _backupBtNorth = DbDataHelper.BAD_VELOCITY;

            // Get the data to plot
            return QueryDataFromDb(cnn, selectedPlotType, minIndex, maxIndex);
        }

        /// <summary>
        /// Query the project database for the data to plot.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="selectedPlotType">Selected Plot Type.</param>
        /// <param name="minIndex">Minimum index.</param>
        /// <param name="maxIndex">Maximum index.</param>
        /// <returns></returns>
        private PlotData QueryDataFromDb(SQLiteConnection cnn, PlotDataType selectedPlotType, int minIndex = 0, int maxIndex = 0)
        {
            // Get the dataset column name
            string datasetColumnName = "EarthVelocityDS";
            switch (selectedPlotType)
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

            // Get data
            string query = string.Format("SELECT ID,EnsembleNum,DateTime,EnsembleDS,AncillaryDS,BottomTrackDS,{0} FROM tblEnsemble WHERE ({1} IS NOT NULL) {2} {3} LIMIT {4} OFFSET {5};",
                                            datasetColumnName,
                                            datasetColumnName,
                                            GenerateQueryFileList(),
                                            GenerateQuerySubsystemList(),
                                            lo.Limit,
                                            lo.Offset);

            // Return the data to plot
            return GetDataFromDb(cnn, numEnsembles, query, selectedPlotType);
        }

        /// <summary>
        /// Get the data from the project DB.
        /// </summary>
        /// <param name="cnn">Database connection.</param>
        /// <param name="numEnsembles">Number of ensembles.</param>
        /// <param name="query">Query string to retreive the data.</param>
        /// <param name="selectedPlotType">Selected Plot Type.</param>
        /// <returns>Magnitude data in (NumEns X NumBin) format.</returns>
        private PlotData GetDataFromDb(SQLiteConnection cnn, int numEnsembles, string query, PlotDataType selectedPlotType)
        {
            // Init list
            PlotData result = new PlotData();
            AreaSeries btSeries = CreateBtSeries();
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
                    if(ensIndex >= numEnsembles)
                    {
                        break;
                    }

                    // Parse the Bottom Track line if enabled
                    if (IsBottomTrackLine)
                    {
                        // Get Bottom Track Line Data
                        ParseBtData(ref btSeries, reader, ensIndex);
                    }

                    // Parse the data from the db
                    // This will be select which type of data to plot
                    double[] data = ParseData(reader, selectedPlotType);

                    // Verify we have data
                    if (data != null)
                    {
                        // If the array has not be created, created now
                        if (result.ProfileData == null)
                        {
                            // Create the array if this is the first entry
                            // NumEnsembles X NumBins
                            result.ProfileData = new double[numEnsembles, data.Length];
                        }

                        // Add the data to the array
                        for (int x = 0; x < data.Length; x++)
                        {
                            result.ProfileData[ensIndex, x] = data[x];
                        }

                        ensIndex++;
                    }
                }
            }

            // Set the Bottom Track series
            result.BottomTrackData = btSeries;

            return result;
        }

        #endregion

        #region Parse Data

        /// <summary>
        /// Select which parser to use based off the selected plot.
        /// </summary>
        /// <param name="reader">Reader holds a single row (ensemble).</param>
        /// <param name="selectedPlotType">Selected Plot Type.</param>
        /// <returns>Data selected for the row.</returns>
        private double[] ParseData(DbDataReader reader, PlotDataType selectedPlotType)
        {
            switch (selectedPlotType)
            {
                case PlotDataType.Magnitude:
                    return ParseMagData(reader);
                case PlotDataType.Direction:
                    return ParseDirData(reader);
                case PlotDataType.Amplitude:
                    return ParseAmpData(reader);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double[] ParseMagData(DbDataReader reader)
        {
            try
            {
                // Get data
                DbDataHelper.VelocityMagDir velMagDir = DbDataHelper.CreateVelocityVectors(reader, _backupBtEast, _backupBtNorth, _IsRemoveShipSpeed, _IsMarkBadBelowBottom);

                // Store the backup value
                if (velMagDir.IsBtVelGood)
                {
                    _backupBtEast = velMagDir.BtEastVel;
                    _backupBtNorth = velMagDir.BtNorthVel;
                }

                return velMagDir.Magnitude;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Earth Velocity Magnitude data row", e);
                return null;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Direction data for a row.</returns>
        private double[] ParseDirData(DbDataReader reader)
        {
            try
            {
                // Get data
                DbDataHelper.VelocityMagDir velMagDir = DbDataHelper.CreateVelocityVectors(reader, _backupBtEast, _backupBtNorth, _IsRemoveShipSpeed, _IsMarkBadBelowBottom);

                // Store the backup value
                if (velMagDir.IsBtVelGood)
                {
                    _backupBtEast = velMagDir.BtEastVel;
                    _backupBtNorth = velMagDir.BtNorthVel;
                }

                return velMagDir.DirectionYNorth;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Earth Velocity Direction data row", e);
                return null;
            }
        }

        /// <summary>
        /// Process the row from the DB.  A row represents an ensemble.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Magnitude data for a row.</returns>
        private double[] ParseAmpData(DbDataReader reader)
        {
            try
            {
                // Get Range Bin if marking bad below bottom
                int rangeBin = BAD_BOTTOM_BIN;
                if(IsMarkBadBelowBottom)
                {
                    //rangeBin = GetRangeBin(reader);
                    rangeBin = DbDataHelper.GetRangeBin(reader);
                    if (rangeBin == DbDataHelper.BAD_BOTTOM_BIN && _prevGoodBottomBin != DbDataHelper.BAD_BOTTOM_BIN)
                    {
                        // Use the backup value if bad
                        rangeBin = _prevGoodBottomBin;
                    }

                    // Store as backup
                    if (rangeBin != DbDataHelper.BAD_BOTTOM_BIN)
                    {
                        _prevGoodBottomBin = rangeBin;
                    }
                }

                // Get the data as a JSON string
                string jsonData = reader["AmplitudeDS"].ToString();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    // Convert to a JSON object
                    JObject ensData = JObject.Parse(jsonData);

                    // Get the number of bins
                    int numBins = ensData["NumElements"].ToObject<int>();
                    int numBeams = ensData["ElementsMultiplier"].ToObject<int>();

                    double[] data = new double[numBins];
                    for (int bin = 0; bin < numBins; bin++)
                    {
                        int avgCnt = 0;
                        double avg = 0.0;

                        // Average the amplitude for each beam data together
                        for (int beam = 0; beam < numBeams; beam++)
                        {
                            if (ensData["AmplitudeData"][bin][beam].ToObject<double>() != BAD_VELOCITY)
                            {
                                avgCnt++;
                                avg += ensData["AmplitudeData"][bin][beam].ToObject<double>();
                            }
                        }

                        // Check if Mark bad below bottom
                        if (_IsMarkBadBelowBottom && rangeBin > BAD_BOTTOM_BIN && bin >= rangeBin)
                        {
                            // Mark bad below bottom
                            data[bin] = BAD_AMPLITUDE;
                        }
                        // Add average data to the array
                        else if (avgCnt > 0)
                        {
                            data[bin] = avg/avgCnt;
                        }
                        else
                        {
                            data[bin] = BAD_AMPLITUDE;
                        }
                    }

                    return data;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing the Amplitude data row", e);
                return null;
            }
        }

        #endregion

        #region Create Plot

        /// <summary>
        /// Create the plot.
        /// </summary>
        /// <returns>Plot created.</returns>
        private ViewResolvingPlotModel CreatePlot()
        {
            ViewResolvingPlotModel temp = new ViewResolvingPlotModel();
            //temp.Background = OxyColors.Black;

            // Color Legend
            var linearColorAxis1 = new LinearColorAxis();
            linearColorAxis1.HighColor = OxyColors.Black;
            linearColorAxis1.LowColor = OxyColors.Black;
            linearColorAxis1.Palette = OxyPalettes.Jet(64);
            linearColorAxis1.Position = AxisPosition.Right;
            linearColorAxis1.Minimum = CurrentMinValue;
            linearColorAxis1.Maximum = CurrentMaxValue;
            linearColorAxis1.Key = COLOR_LEGEND_KEY;
            temp.Axes.Add(linearColorAxis1);

            // Bottom Axis 
            // Ensembles 
            var linearAxis2 = new LinearAxis();
            linearAxis2.Position = AxisPosition.Bottom;
            linearAxis2.Unit = "Ensembles";
            linearAxis2.Key = "Ensembles";
            temp.Axes.Add(linearAxis2);

            // Left axis in Bins
            _binAxis = CreatePlotAxis(AxisPosition.Left, "bins");
            _binAxis.AxisChanged += BinAxis_AxisChanged;
            _binAxis.Key = "BinAxis";
            temp.Axes.Add(_binAxis);

            // Left axis in Meters next to bins
            _depthAxis = CreatePlotAxis(AxisPosition.Left, "meters", 2);
            _depthAxis.Key = "DepthAxis";
            temp.Axes.Add(_depthAxis);

            return temp;
        }

        /// <summary>
        /// Handle changes to the bin axis.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BinAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            // Get the current selections
            double minBin = _binAxis.ActualMinimum;
            double maxBin = _binAxis.ActualMaximum;

            // Set the axis for the depth axis
            _depthAxis.Minimum = _blankSize + (minBin * _binSize);
            _depthAxis.Maximum = _blankSize + (maxBin * _binSize);
        }

        /// <summary>
        /// Create the plot axis.  Set the values for the plot axis.
        /// If you do not want to set a value, set the value to NULL.
        /// </summary>
        /// <param name="position">Position of the axis.</param>
        /// <param name="positionTier">Where to place the axis.</param>
        /// <param name="unit">Label for the axis.</param>
        /// <returns>LinearAxis for the plot.</returns>
        private LinearAxis CreatePlotAxis(AxisPosition position, string unit, int positionTier = 0)
        {
            // Create the axis
            LinearAxis axis = new LinearAxis();

            // Standard options
            //axis.TicklineColor = OxyColors.White;
            //axis.MajorGridlineStyle = LineStyle.Solid;
            //axis.MinorGridlineStyle = LineStyle.Solid;
            //axis.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White);
            //axis.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White);
            axis.EndPosition = 0;
            axis.StartPosition = 1;
            axis.Position = position;
            axis.Key = unit;
            axis.PositionTier = positionTier;

            // Set the axis label
            axis.Unit = unit;

            return axis;
        }

        /// <summary>
        /// Set the minimum and maximum value for the color legend.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        private void SetMinMaxColorAxis(double min, double max)
        {
            // Find the LinearColorAxis to set the min and max value
            foreach(var axis in Plot.Axes)
            {
                if(axis.GetType() == typeof(LinearColorAxis))
                {
                    ((LinearColorAxis)axis).Minimum = min;
                    ((LinearColorAxis)axis).Maximum = max;

                    // Update the plot
                    Plot.InvalidatePlot(true);
                }
            }
        }

        #endregion

        #region Draw Plot

        /// <summary>
        /// Draw the plot based off the settings.
        /// </summary>
        /// <param name="fileName">File name of the project.</param>
        /// <param name="selectedPlotType">Selected plot type.</param>
        /// <param name="minIndex">Minimum index to draw.</param>
        /// <param name="maxIndex">Maximum index to draw.</param>
        protected async void DrawPlot(string fileName, PlotDataType selectedPlotType, int minIndex = 0, int maxIndex = 0)
        {
            // Data to get from the project
            PlotData data = null;

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
                            // Run as a task to allow the UI to be updated
                            // Use await to keep the sqlite connection open
                            await Task.Run(() =>TotalNumEnsembles = GetNumEnsembles(sqlite_conn));

                            // If this is the first time loading
                            // show the entire plot
                            if (_firstLoad)
                            {
                                _firstLoad = false;
                                minIndex = 1;
                                maxIndex = TotalNumEnsembles;

                                // Set the Bin size and blank
                                SetBinSizeAndBlank(sqlite_conn);
                            }

                            // Get the magnitude data
                            // Run as a task to allow the UI to be updated
                            // Use await to keep the sqlite connection open
                            await Task.Run(() => data = GetData(sqlite_conn, TotalNumEnsembles, selectedPlotType, minIndex, maxIndex));

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
                        await Task.Run(() => PlotProfileData(data.ProfileData));

                        // Plot the Bottom Track data from the project
                        await Task.Run(() => PlotBtSeries(data.BottomTrackData));

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

        #region Plot Data

        /// <summary>
        /// Plot the given data.
        /// </summary>
        /// <param name="data">Data to plot by creating a series.</param>
        private void PlotProfileData(double[,] data)
        {
            // Update the plots in the dispatcher thread
            try
            {
                // Lock the plot for an update
                lock (Plot.SyncRoot)
                {
                    // Clear any current series
                    StatusMsg = "Clear old Plot";
                    Plot.Series.Clear();

                    // Create a heatmap series
                    HeatMapSeries series = new HeatMapSeries();
                    series.X0 = 0;                          // Left starts 0
                    series.X1 = data.GetLength(0);          // Right (num ensembles)
                    series.Y0 = 0;                          // Top starts 0
                    series.Y1 = data.GetLength(1);          // Bottom end (num bins)
                    series.Data = data;
                    series.Interpolate = false;

                    // Add the series to the plot
                    StatusMsg = "Add Plot data";
                    Plot.Series.Add(series);
                }
            }
            catch (Exception ex)
            {
                // When shutting down, can get a null reference
                Debug.WriteLine("Error updating Heatmap Plot", ex);
            }

            // After the line series have been updated
            // Refresh the plot with the latest data.
            StatusMsg = "Drawing Plot";
            Plot.InvalidatePlot(true);

            if (data != null)
            {
                StatusMsg = "Drawing complete.  Total Ensembles: " + data.GetLength(0);
            }
            else
            {
                StatusMsg = "Drawing complete.";
            }
        }


        #endregion

        #region Replot Data

        /// <summary>
        /// Replot the data based off a settings chage
        /// </summary>
        /// <param name="eplotDataType"></param>
        public void ReplotData(PlotDataType eplotDataType)
        {
            switch(eplotDataType)
            {
                case PlotDataType.Magnitude:
                    _SelectedPlotType = PlotDataType.Magnitude;
                    NotifyOfPropertyChange(() => SelectedPlotType);
                    IsAmplitude = false;
                    IsDirection = false;
                    SetMinMaxColorAxis(0, 2);
                    Plot.Title = "Water Magnitude";
                    break;
                case PlotDataType.Direction:
                    _SelectedPlotType = PlotDataType.Direction;
                    NotifyOfPropertyChange(() => SelectedPlotType);
                    IsAmplitude = false;
                    IsMagnitude = false;
                    SetMinMaxColorAxis(0, 360);
                    Plot.Title = "Water Direction";
                    break;
                case PlotDataType.Amplitude:
                    _SelectedPlotType = PlotDataType.Amplitude;
                    NotifyOfPropertyChange(() => SelectedPlotType);
                    IsMagnitude = false;
                    IsDirection = false;
                    SetMinMaxColorAxis(0, 120);
                    Plot.Title = "Amplitude";
                    break;
                default:
                    break;
            }

            // Replot the data
            if (!string.IsNullOrEmpty(ProjectFilePath))
            {
                DrawPlot(ProjectFilePath, eplotDataType);
            }

        }

        /// <summary>
        /// Update the min and max ensembles selected.
        /// </summary>
        /// <param name="minIndex">Minimum ensemble index.</param>
        /// <param name="maxIndex">Maximum ensemble index.</param>
        public override void ReplotData(int minIndex, int maxIndex)
        {
            // Replot the data
            if (!string.IsNullOrEmpty(ProjectFilePath))
            {
                DrawPlot(ProjectFilePath, _SelectedPlotType, minIndex, maxIndex);
            }
        }

        /// <summary>
        /// Update plot.
        /// </summary>
        public override void ReplotData()
        {
            // Replot the data
            if (!string.IsNullOrEmpty(ProjectFilePath))
            {
                DrawPlot(ProjectFilePath, _SelectedPlotType);
            }
        }

        #endregion

        #region Bottom Track Line

        /// <summary>
        /// Add Bottom Track line series.  This will be a line to mark the bottom.
        /// </summary>
        private void PlotBtSeries(AreaSeries series)
        {
            // Lock the plot for an update
            lock (Plot.SyncRoot)
            {
                Plot.Series.Add(series);
            }

            // Then refresh the plot
            Plot.InvalidatePlot(true);
        }

        /// <summary>
        /// Create the Bottom Track series.
        /// </summary>
        /// <returns>Series for Bottom Track line.</returns>
        private AreaSeries CreateBtSeries()
        {
            // Add the series to the list
            // Create a series
            AreaSeries series = new AreaSeries()
            {
                Color = OxyColors.Red,
                Color2 = OxyColors.Transparent,
                Fill = OxyColor.FromAColor(240, OxyColors.DarkGray),
            };
            series.Tag = "Bottom Track";

            return series;
        }

        /// <summary>
        /// Update the Bottom Track series with the latest depth.
        /// </summary>
        /// <param name="btSeries">Bottom Track series to update.</param>
        /// <param name="reader">Database reader.</param>
        /// <param name="ensIndex">Index in the plot based off the reader.</param>
        private void ParseBtData(ref AreaSeries btSeries, DbDataReader reader, int ensIndex)
        {
            try
            {
                // Convert to a JSON object
                string jsonEnsemble = reader["EnsembleDS"].ToString();
                JObject ensData = JObject.Parse(jsonEnsemble);
                int numBins = ensData["NumBins"].ToObject<int>();

                // Update the bottom track line series
                //int rangeBin = GetRangeBin(reader);
                int rangeBin = DbDataHelper.GetRangeBin(reader);
                if (rangeBin == DbDataHelper.BAD_BOTTOM_BIN && _prevGoodBottomBin != DbDataHelper.BAD_BOTTOM_BIN)
                {
                    // Use the backup value if bad
                    rangeBin = _prevGoodBottomBin;
                }

                // Store as backup
                if (rangeBin != DbDataHelper.BAD_BOTTOM_BIN)
                {
                    _prevGoodBottomBin = rangeBin;
                }


                // Only plot the range if it is found
                if (rangeBin > 0)
                {
                    // Create a new data point for the bottom track line
                    // This will be the (ensemble count, range bin)
                    btSeries.Points.Add(new DataPoint(ensIndex, rangeBin));

                    // Add the second point for the shaded area
                    if (rangeBin < numBins)
                    {
                        // Less then the number of bins, so go to the end of the number of bins
                        btSeries.Points2.Add(new DataPoint(ensIndex, numBins));
                    }
                    else
                    {
                        // This is the deepest point
                        btSeries.Points2.Add(new DataPoint(ensIndex, rangeBin));
                    }
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine("Error parsing Bottom Track data", e);
                return;
            }
        }

        #endregion

        #region Bin Size and Blank

        /// <summary>
        /// Get the bin size and blank of the current project.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        private void SetBinSizeAndBlank(SQLiteConnection cnn)
        {
            try
            {
                string query = string.Format("SELECT AncillaryDS FROM {0} LIMIT 1;", "tblEnsemble");

                // Ensure a connection was made
                if (cnn == null)
                {
                    return;
                }

                using (DbCommand cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = query;

                    // Get Result
                    DbDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        // Get the ancillary data
                        string jsonAncillary = reader["AncillaryDS"].ToString();

                        if(!string.IsNullOrEmpty(jsonAncillary))
                        {
                            // Parse JSON
                            JObject ancData = JObject.Parse(jsonAncillary);

                            // Get Bin Size and First bin
                            _binSize = ancData["BinSize"].ToObject<double>();
                            _blankSize = ancData["FirstBinRange"].ToObject<double>();

                            // Set the major step based off the bin size
                            if (_binSize > 0)
                            {
                                _depthAxis.MajorStep = _binSize * 2.0;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error getting the bin size and blank.", e);
            }
        }

        #endregion

        #region Palette List

        /// <summary>
        /// Create the palette list.
        /// </summary>
        private void CreatePaletteList()
        {
            PaletteList = new BindingList<OxyPalette>();
            PaletteList.Add(OxyPalettes.BlackWhiteRed(64));
            PaletteList.Add(OxyPalettes.BlueWhiteRed(64));
            PaletteList.Add(OxyPalettes.BlueWhiteRed31);
            PaletteList.Add(OxyPalettes.Cool(64));
            PaletteList.Add(OxyPalettes.Gray(64));
            PaletteList.Add(OxyPalettes.Hot(64));
            PaletteList.Add(OxyPalettes.Hue64);
            PaletteList.Add(OxyPalettes.HueDistinct(64));
            PaletteList.Add(OxyPalettes.Jet(64));
            PaletteList.Add(OxyPalettes.Rainbow(64));
        }

        #endregion
    }
}
