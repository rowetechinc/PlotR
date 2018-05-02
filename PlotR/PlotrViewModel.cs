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
        #region Variables

        /// <summary>
        /// Selected Plot type.
        /// </summary>
        private HeatmapPlotViewModel.PlotDataType _SelectedPlotType;

        #endregion

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
        public HeatmapPlotViewModel Heatmap { get; set; }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to select an SQLite file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

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

            // Create the Heatmap
            Heatmap = IoC.Get<HeatmapPlotViewModel>();

            // Display the plot
            ActivateItem(IoC.Get<HeatmapPlotViewModel>());

            // Open file commands
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
        private async void LoadProject(string fileName)
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
                            await Task.Run(() => TotalEnsembles = GetNumEnsembles(sqlite_conn));

                            // Reset the min and max
                            CurrentMinValue = 1;
                            CurrentMaxValue = TotalEnsembles;

                            // Get the magnitude data
                            //await Task.Run(() => data = GetData(sqlite_conn, TotalNumEnsembles, selectedPlotType));

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
            if (Heatmap != null)
            {
                Heatmap.LoadProject(FileName, HeatmapPlotViewModel.PlotDataType.Magnitude, _CurrentMinValue, _CurrentMaxValue);     // Default use magnitude
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void UpdateEnsembleSelections()
        {
            if (Heatmap != null)
            {
                await Task.Run(() => Heatmap.ReplotData(_CurrentMinValue, _CurrentMaxValue));
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void UpdateEnsembleSelections(int min, int max)
        {
            if (Heatmap != null)
            {
                _CurrentMinValue = min;
                NotifyOfPropertyChange(() => CurrentMinValue);
                _CurrentMaxValue = max;
                NotifyOfPropertyChange(() => CurrentMaxValue);

                await Task.Run(() => Heatmap.ReplotData(min, max));
            }
        }

        /// <summary>
        /// Update the plot with the new ensembles selected.
        /// </summary>
        public async void DisplayAll()
        {
            if (Heatmap != null)
            {
                CurrentMinValue = 1;
                CurrentMaxValue = TotalEnsembles;
                await Task.Run(() => Heatmap.ReplotData(_CurrentMinValue, _CurrentMaxValue));
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
