using Caliburn.Micro;
using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
    class PlotrViewModel : Conductor<object>
    {
        #region Properties

        #region File name

        /// <summary>
        /// File name of sqlite file.
        /// </summary>
        private string _FileName;
        /// <summary>
        /// File name of sqlite file.
        /// </summary>
        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                NotifyOfPropertyChange(() => FileName);
            }
        }

        #endregion

        #region Slider Selection

        /// <summary>
        /// Total Ensembles in the project.
        /// </summary>
        private int _TotalEnsembles;
        /// <summary>
        /// Total Ensembles in the project.
        /// </summary>
        public int TotalEnsembles
        {
            get { return _TotalEnsembles; }
            set
            {
                _TotalEnsembles = value;
                NotifyOfPropertyChange(() => TotalEnsembles);
            }
        }

        /// <summary>
        /// Minimum selection to display data.
        /// </summary>
        private int _CurrentMinValue;
        /// <summary>
        /// Minimum selection to display data.
        /// </summary>
        public int CurrentMinValue
        {
            get { return _CurrentMinValue; }
            set
            {
                if (value < _CurrentMaxValue)
                {
                    _CurrentMinValue = value;
                    NotifyOfPropertyChange(() => CurrentMinValue);

                    // Update the ensemble selections
                    //UpdateEnsembleSelections();
                }
            }
        }

        /// <summary>
        /// Maximum selection to display data.
        /// </summary>
        private int _CurrentMaxValue;
        /// <summary>
        /// Maximum selection to display data.
        /// </summary>
        public int CurrentMaxValue
        {
            get { return _CurrentMaxValue; }
            set
            {
                if (value > _CurrentMinValue)
                {
                    _CurrentMaxValue = value;
                    NotifyOfPropertyChange(() => CurrentMaxValue);

                    // Update the ensemble selections
                    //UpdateEnsembleSelections();
                }
            }
        }

        #endregion

        #region Plot

        /// <summary>
        /// Heatmap plot.
        /// </summary>
        public HeatmapPlotViewModel HeatmapPlot { get; set; }

        /// <summary>
        /// Time Series plot.
        /// </summary>
        public TimeSeriesViewModel TimeseriesPlot { get; set; }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to select an SQLite file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

        /// <summary>
        /// Exit the application
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExitCommand { get; private set; }

        /// <summary>
        /// View the Heatmap plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> HeatmapCommand { get; private set; }

        /// <summary>
        /// View the times series plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> TimeseriesCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initialize
        /// </summary>
        public PlotrViewModel()
        {
            // Initialize value
            CurrentMinValue = 0;
            CurrentMaxValue = 100;
            TotalEnsembles = 100;

            // Create the plots
            HeatmapPlot = new HeatmapPlotViewModel();       // Create VM
            IoC.BuildUp(HeatmapPlot);                       // Add to the container
            TimeseriesPlot = new TimeSeriesViewModel();     // Create VM
            IoC.BuildUp(TimeseriesPlot);                    // Add to the container

            // Open file commands
            this.OpenCommand = ReactiveCommand.Create(() => OpenFile());

            // Exit command
            this.ExitCommand = ReactiveCommand.Create(() => System.Windows.Application.Current.Shutdown());

            // Load the plots
            this.HeatmapCommand = ReactiveCommand.Create(() => ActivateItem(HeatmapPlot));
            this.TimeseriesCommand = ReactiveCommand.Create(() => ActivateItem(TimeseriesPlot));

            // Display the plot
            ActivateItem(HeatmapPlot);
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
                // Set the file name
                FileName = openFileDialog.FileName;

                // Load Project settings
                LoadProject(FileName);

                // Load plots
                LoadPlots();
            }
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project to display the plot.
        /// </summary>
        /// <param name="fileName">File name of the project.</param>
        private void LoadProject(string fileName)
        {
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
                            TotalEnsembles = GetNumEnsembles(sqlite_conn);

                            // Reset the min and max
                            CurrentMinValue = 1;
                            CurrentMaxValue = TotalEnsembles;

                            // Close connection
                            sqlite_conn.Close();
                        }
                    }
                    catch (SQLiteException e)
                    {
                        Debug.WriteLine("Error using database to read project info.", e);
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Error using database to read project info", e);
                        return;
                    }
                }
            }
        }

        #endregion

        #region Load Plots

        /// <summary>
        /// Load the plots.
        /// </summary>
        private void LoadPlots()
        {
            //foreach (var vm in IoC.GetAll<IPlotViewModel>())
            //{
            //    vm.LoadProject(FileName, _CurrentMinValue, _CurrentMaxValue);
            //}

            if (HeatmapPlot != null)
            {
                HeatmapPlot.LoadProject(FileName, _CurrentMinValue, _CurrentMaxValue);
            }
            if (TimeseriesPlot != null)
            {
                TimeseriesPlot.LoadProject(FileName, _CurrentMinValue, _CurrentMaxValue);
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void UpdateEnsembleSelections()
        {
            if (HeatmapPlot != null)
            {
                await Task.Run(() => HeatmapPlot.ReplotData(_CurrentMinValue, _CurrentMaxValue));
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void UpdateEnsembleSelections(int min, int max)
        {
            if (HeatmapPlot != null)
            {
                _CurrentMinValue = min;
                NotifyOfPropertyChange(() => CurrentMinValue);
                _CurrentMaxValue = max;
                NotifyOfPropertyChange(() => CurrentMaxValue);

                await Task.Run(() => HeatmapPlot.ReplotData(min, max));
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void DisplayAll()
        {
            if (HeatmapPlot != null)
            {
                CurrentMinValue = 1;
                CurrentMaxValue = TotalEnsembles;
                await Task.Run(() => HeatmapPlot.ReplotData(_CurrentMinValue, _CurrentMaxValue));
            }
        }

        #endregion

        #region Get Number of Ensembles

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

        #endregion
    }
}
