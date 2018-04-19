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

namespace PlotR {
    public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell
    {

        #region Class 

        /// <summary>
        /// Earth Velocity Dataset.
        /// </summary>
        public class EarthVelocityDS
        {
            public int NumElements { get; set; }
            public int ElementMultiplier { get; set; }
            public float[] VelcocityVector { get; set; }
            public float[,] EarthVelocityData { get; set; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// File name of sqlite file.
        /// </summary>
        private string _FileName;

        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                NotifyOfPropertyChange(() => FileName);
            }
        }

        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        private PlotModel _plot;
        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        public PlotModel Plot
        {
            get { return _plot; }
            set
            {
                _plot = value;
                this.NotifyOfPropertyChange(() => this.Plot);
            }
        }

        /// <summary>
        /// Number of ensembles.
        /// </summary>
        private int _NumEnsembles;

        public int NumEnsembles
        {
            get { return _NumEnsembles; }
            set
            {
                _NumEnsembles = value;
                NotifyOfPropertyChange(() => NumEnsembles);
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to select an SQLite file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initialize.
        /// </summary>
        public ShellViewModel()
        {
            // Initialize
            FileName = "Open a DB file...";
            Plot = CreatePlot();

            // Setup commands
            this.OpenCommand = ReactiveCommand.Create(() => OpenFile());

        }

        #region Open File

        /// <summary>
        /// Select the file to open.
        /// </summary>
        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "project db files (*.db)|*.db|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                FileName = openFileDialog.FileName;

                // Load the project
                LoadProject(FileName);
            }
        }

        #endregion

        #region Create Plot

        /// <summary>
        /// Create the plot.  This will create a base plot.
        /// Then based off the series type, it will add the
        /// series type specifics to the plot.  This includes
        /// the axis labels and min and max values.
        /// </summary>
        /// <param name="type">Series type.</param>
        /// <returns>Plot created based off the series type.</returns>
        private PlotModel CreatePlot()
        {
            PlotModel temp = new PlotModel();

            //temp.TextColor = OxyColors.White;
            //temp.Background = OxyColors.Black;

            // Color Legend
            var linearColorAxis1 = new LinearColorAxis();
            linearColorAxis1.HighColor = OxyColors.Black;
            linearColorAxis1.LowColor = OxyColors.Black;
            linearColorAxis1.Palette = OxyPalettes.Jet(64);
            linearColorAxis1.Position = AxisPosition.Right;
            linearColorAxis1.Minimum = 0.0;
            linearColorAxis1.Maximum = 2.0;
            temp.Axes.Add(linearColorAxis1);

            // Bottom Axis 
            // Ensembles 
            var linearAxis2 = new LinearAxis();
            linearAxis2.Position = AxisPosition.Bottom;
            //linearAxis2.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White);
            //linearAxis2.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White);
            linearAxis2.Unit = "Ensembles";
            linearAxis2.Key = "Ensembles";
            temp.Axes.Add(linearAxis2);

            // Set the plot title
            temp.Title = "Magnitude";

            // Left axis in Bins
            temp.Axes.Add(CreatePlotAxis(AxisPosition.Left, "bins"));

            // Right axis in Meters
            temp.Axes.Add(CreatePlotAxis(AxisPosition.Left, "meters", 2));

            return temp;
        }

        /// <summary>
        /// Create the plot axis.  Set the values for the plot axis.
        /// If you do not want to set a value, set the value to NULL.
        /// </summary>
        /// <param name="position">Position of the axis.</param>
        /// <param name="majorStep">Minimum value.</param>
        /// <param name="unit">Label for the axis.</param>
        /// <returns>LinearAxis for the plot.</returns>
        private LinearAxis CreatePlotAxis(AxisPosition position, string unit, int positionTier = 0)
        {
            // Create the axis
            LinearAxis axis = new LinearAxis();

            // Standard options
            axis.TicklineColor = OxyColors.White;
            axis.MajorGridlineStyle = LineStyle.Solid;
            axis.MinorGridlineStyle = LineStyle.Solid;
            axis.MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White);
            axis.MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White);
            axis.EndPosition = 0;
            axis.StartPosition = 1;
            axis.Position = position;
            axis.Key = unit;
            axis.PositionTier = positionTier;

            // Set the axis label
            axis.Unit = unit;

            return axis;
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project and get all the data.
        /// </summary>
        /// <param name="fileName"></param>
        private void LoadProject(string fileName)
        {
            // Data to get from the project
            double[,] data = null;

            // Create data Source string
            string dataSource = string.Format("Data Source={0};Version=3;", fileName);

            try
            {
                // Create a new database connection:
                using (SQLiteConnection sqlite_conn = new SQLiteConnection(dataSource))
                {
                    // Open the connection:
                    sqlite_conn.Open();

                    // Get number of ensembles
                    NumEnsembles = GetNumEnsembles(sqlite_conn);

                    // Get the magnitude data
                    data = GetMagnitude(sqlite_conn, NumEnsembles);

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

            if(data != null)
            {
                // Plot the data from the project
                PlotData(data);
            }
        }

        /// <summary>
        /// Get the number of ensembles in the project database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <returns>Number of ensembles.</returns>
        private int GetNumEnsembles(SQLiteConnection cnn)
        {
            string query = string.Format("SELECT COUNT(*) FROM {0};", "tblEnsemble");

            // Ensure a connection was made
            if (cnn == null)
            {
                return -1;
            }

            int result = 0;
            using (DbCommand cmd = cnn.CreateCommand())
            {
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                result = Convert.ToInt32(resultValue.ToString());

            }

            return result;
        }

        /// <summary>
        /// Get the Magnitude data from the project DB.
        /// </summary>
        /// <param name="cnn">Database connection.</param>
        /// <param name="numEnsembles">Number of ensembles.</param>
        /// <returns>Magnitude data in (NumEns X NumBin) format.</returns>
        private double[,] GetMagnitude(SQLiteConnection cnn, int numEnsembles)
        {
            // Init list
            double[,] magData = null;
            int ensIndex = 0;

            string query = string.Format("SELECT ID,EnsembleNum,DateTime,EarthVelocityDS FROM tblEnsemble;");

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
                while(reader.Read())
                {
                    // Convert the Earth JSON to an object
                    //Debug.WriteLine(reader["EnsembleNum"]);

                    // Parse the data from the db
                    double[] data = ParseMagData(reader);

                    // If the array has not be created, created now
                    if(magData == null)
                    {
                        // Create the array if this is the first entry
                        // NumEnsembles X NumBins
                        magData = new double[numEnsembles, data.Length];
                    }

                    // Add the data to the array
                    for(int x = 0; x < data.Length; x++)
                    {
                        magData[ensIndex, x] = data[x];
                    }

                    ensIndex++;
                }
            }

            return magData;
        }

        /// <summary>
        /// Get all the magnitude data from the project DB.
        /// </summary>
        /// <param name="reader">Database connection data.</param>
        /// <returns>Magnitude data.</returns>
        private double[] ParseMagData(DbDataReader reader)
        {
            try
            {
                // Get the earth data as a JSON string
                string jsonEarth = reader["EarthVelocityDS"].ToString();

                // Convert to a JSON object
                JObject ensEarth = JObject.Parse(jsonEarth);

                // Get the number of bins
                int numBins = ensEarth["NumElements"].ToObject<int>();
                //Debug.WriteLine("Num Bins: " + numBins);

                //Debug.WriteLine(ensEarth["VelocityVectors"][0]["Magnitude"]);
                double[] data = new double[numBins];
                for(int x = 0; x < numBins; x++)
                {
                    // Get the velocity vector magntidue from the JSON object and add it to the array
                    data[x] = ensEarth["VelocityVectors"][x]["Magnitude"].ToObject<double>();
                }

                return data;
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error parsing the data", e);
                return null;
            }


        }

        #endregion

        #region Plot Data

        /// <summary>
        /// Plot the given data.
        /// </summary>
        /// <param name="data">Data to plot by creating a series.</param>
        private void PlotData(double[,] data)
        {
            // Update the plots in the dispatcher thread
            try
            {
                // Lock the plot for an update
                lock (Plot.SyncRoot)
                {
                    // Clear any current series
                    Plot.Series.Clear();

                    // Create a heatmap series
                    HeatMapSeries series = new HeatMapSeries();
                    series.Data = data;
                    series.Title = "Magnitude";
                    series.X0 = 0;                          // Left starts 0
                    series.X1 = data.Length;                // Right (num ensembles)
                    series.Y0 = 0;                          // Top starts 0
                    series.Y1 = data.GetLength(1);          // Bottom end (num bins)

                    // Add the series to the plot
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
            Plot.InvalidatePlot(true);
        }


            #endregion

        }
}