using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{
    public class PlotViewModel : Caliburn.Micro.Screen, IPlotViewModel
    {

        #region Variables

        /// <summary>
        /// Max Ensembles to display.
        /// </summary>
        ///private int MAX_ENS = 510000;

        /// <summary>
        /// Bad velocity value.
        /// </summary>
        protected double BAD_VELOCITY = 88.888;

        /// <summary>
        /// Bad amplitude.
        /// </summary>
        protected double BAD_AMPLITUDE = 0.0;

        /// <summary>
        /// Bad Bottom Value.
        /// </summary>
        protected int BAD_BOTTOM_BIN = 0;

        /// <summary>
        /// If this is the first time loading the project, then set the max index to the total
        /// number of ensembles.  This flag is reset anytime a new project is loaded.
        /// </summary>
        protected bool _firstLoad;

        /// <summary>
        /// Bin size for the current project.
        /// </summary>
        protected double _binSize;

        /// <summary>
        /// Blank size for the current project.
        /// </summary>
        protected double _blankSize;

        #endregion

        #region Sturct

        /// <summary>
        /// Limit and offset value.
        /// </summary>
        public struct LimitOffset
        {
            /// <summary>
            /// Limit is the number of ensembles.
            /// </summary>
            public int Limit { get; set; }

            /// <summary>
            /// Offset is the start location.
            /// </summary>
            public int Offset { get; set; }
        }

        #endregion

        #region Properties

        #region Plot

        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        protected ViewResolvingPlotModel _plot;
        /// <summary>
        /// The plot for the view model.  This will be the plot
        /// that will be updated by the user.
        /// </summary>
        public ViewResolvingPlotModel Plot
        {
            get { return _plot; }
            set
            {
                _plot = value;
                this.NotifyOfPropertyChange(() => this.Plot);
            }
        }

        #endregion

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

        #region Status

        /// <summary>
        /// File name of sqlite file.
        /// </summary>
        protected string _ProjectFilePath;

        public string ProjectFilePath
        {
            get { return _ProjectFilePath; }
            set
            {
                _ProjectFilePath = value;
                NotifyOfPropertyChange(() => ProjectFilePath);
            }
        }

        /// <summary>
        /// Number of ensembles.
        /// </summary>
        protected int _TotalNumEnsembles;
        /// <summary>
        /// Number of ensembles.
        /// </summary>
        public int TotalNumEnsembles
        {
            get { return _TotalNumEnsembles; }
            set
            {
                _TotalNumEnsembles = value;
                NotifyOfPropertyChange(() => TotalNumEnsembles);
            }
        }

        /// <summary>
        /// Status Message.
        /// </summary>
        protected string _StatusMsg;
        /// <summary>
        /// Status Message.
        /// </summary>
        public string StatusMsg
        {
            get { return _StatusMsg; }
            set
            {
                _StatusMsg = value;
                NotifyOfPropertyChange(() => StatusMsg);
            }
        }

        /// <summary>
        /// Status Progress count.
        /// </summary>
        protected int _StatusProgress;
        /// <summary>
        /// Status progress count.
        /// </summary>
        public int StatusProgress
        {
            get { return _StatusProgress; }
            set
            {
                _StatusProgress = value;
                NotifyOfPropertyChange(() => StatusProgress);
            }
        }

        /// <summary>
        /// Status Progress max count.
        /// </summary>
        protected int _StatusProgressMax;
        /// <summary>
        /// Status progress max count.
        /// </summary>
        public int StatusProgressMax
        {
            get { return _StatusProgressMax; }
            set
            {
                _StatusProgressMax = value;
                NotifyOfPropertyChange(() => StatusProgressMax);
            }
        }

        #endregion

        #region File and Subsystem List

        /// <summary>
        /// List of all the files in the project.
        /// </summary>
        public BindingList<MenuItemRti> ProjectFileList { get; set; }

        /// <summary>
        /// List of all the Subsystem configurations.
        /// </summary>
        public BindingList<MenuItemRtiSubsystem> SubsystemConfigList { get; set; }

        #endregion

        #endregion


        #region Commands

        /// <summary>
        /// Command to select an SQLite file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

        /// <summary>
        /// File selection changed.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FileSelectionCommand { get; private set; }

        /// <summary>
        /// Subsystem selection changed.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SubsystemSelectionCommand { get; private set; }

        #endregion

        public PlotViewModel()
        {
            // Initialize
            _firstLoad = true;
            _binSize = 0.0;
            _blankSize = 0.0;

            // Status
            StatusMsg = "Open a DB file...";
            StatusProgress = 0;
            StatusProgressMax = 100;

            // List of project files and subsystem
            ProjectFileList = new BindingList<MenuItemRti>();
            SubsystemConfigList = new BindingList<MenuItemRtiSubsystem>();

            // Setup commands
            this.OpenCommand = ReactiveCommand.Create(() => OpenFile());

            // When a file selection is made
            this.FileSelectionCommand = ReactiveCommand.Create(() => ReplotData());

            // When a subsystem selection is made
            this.SubsystemSelectionCommand = ReactiveCommand.Create(() => ReplotData());
        }

        #region Open File

        /// <summary>
        /// Select the file to open.
        /// </summary>
        protected void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "project db files (*.db)|*.db|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _firstLoad = true;

                // Set the file name
                ProjectFilePath = openFileDialog.FileName;

                // Load the project
                LoadProject(ProjectFilePath);
            }
        }

        #endregion

        #region Load Project

        /// <summary>
        /// Load the project and get all the data.
        /// </summary>
        /// <param name="fileName">File path of the project.</param>
        /// <param name="minIndex">Minimum ensemble index to display.</param>
        /// <param name="maxIndex">Minimum ensemble index to display.</param>
        public virtual void LoadProject(string fileName, int minIndex = 0, int maxIndex = 0)
        {
            // Set the selected values
            _ProjectFilePath = fileName;
            this.NotifyOfPropertyChange(() => this.ProjectFilePath);

            // Reset settings
            _firstLoad = true;

            // Clear the current list and get the new file list from the project
            ProjectFileList.Clear();
            GetFileList(fileName);

            // Clear the current list and get the new subsystem configurations list from the project
            SubsystemConfigList.Clear();
            GetSubsystemConfigList(fileName);
        }

        #endregion

        #region File List

        /// <summary>
        /// Populate the list with all the available unique files in the project.
        /// </summary>
        /// <param name="fileName">File path of project.</param>
        protected void GetFileList(string fileName)
        {
            try
            {
                // Create data Source string
                string dataSource = string.Format("Data Source={0};Version=3;", fileName);

                // Create a new database connection:
                using (SQLiteConnection cnn = new SQLiteConnection(dataSource))
                {
                    // Open the connection:
                    cnn.Open();

                    // Ensure a connection was made
                    if (cnn == null)
                    {
                        return;
                    }

                    // Create a command to query
                    using (DbCommand cmd = cnn.CreateCommand())
                    {
                        string query = string.Format("SELECT DISTINCT {0} FROM {1};", "FileName", "tblEnsemble");
                        cmd.CommandText = query;

                        // Get all the results
                        DbDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader == null)
                            {
                                break;
                            }

                            // Add file name to list
                            ProjectFileList.Add(new MenuItemRti() { Header = reader["FileName"].ToString(), IsCheckable = true, IsChecked = true, Command = FileSelectionCommand });
                        }

                    }
                }
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error using database to get file names", e);
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error using database to get file names", e);
                return;
            }
        }

        /// <summary>
        /// Get the query string to inclue the complete list of selected files.
        /// </summary>
        /// <returns></returns>
        protected string GenerateQueryFileList()
        {
            if (ProjectFileList.Count <= 0)
            {
                return "";
            }

            // Build up the list of selected files
            StringBuilder sb = new StringBuilder();
            foreach (var item in ProjectFileList)
            {
                // If checked, add to list
                if (item.IsChecked)
                {
                    sb.Append(string.Format("'{0}',", item.Header));
                }
            }

            // Convert to string
            string fileList = sb.ToString();

            if (!string.IsNullOrEmpty(fileList))
            {
                // Remove the last comma
                fileList = fileList.Substring(0, fileList.Length - 1);
                return string.Format("AND (FileName IN ({0}))", fileList);
            }
            //else
            //{
            //    // If known of the files are selected, then select all of them again
            //    foreach(var item in ProjectFileList)
            //    {
            //        item.IsChecked = true;
            //        NotifyOfPropertyChange(() => ProjectFileList);
            //    }
            //}

            return "";

        }

        #endregion

        #region Subsystem List

        /// <summary>
        /// Populate the list with all the available unique subsystem configurations in the project.
        /// </summary>
        /// <param name="fileName">File path of project.</param>
        protected void GetSubsystemConfigList(string fileName)
        {
            try
            {
                // Create data Source string
                string dataSource = string.Format("Data Source={0};Version=3;", fileName);

                // Create a new database connection:
                using (SQLiteConnection cnn = new SQLiteConnection(dataSource))
                {
                    // Open the connection:
                    cnn.Open();

                    // Ensure a connection was made
                    if (cnn == null)
                    {
                        return;
                    }

                    // Create a command to query
                    using (DbCommand cmd = cnn.CreateCommand())
                    {
                        string query = string.Format("SELECT DISTINCT Subsystem,CepoIndex FROM {0};", "tblEnsemble");
                        cmd.CommandText = query;

                        // Get all the results
                        DbDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader == null)
                            {
                                break;
                            }

                            // Subsystem
                            string subsystem = reader["Subsystem"].ToString();
                            string cepoIndex = reader["CepoIndex"].ToString();
                            string result = string.Format("[{0}]-{1}", subsystem, cepoIndex);

                            // Add file name to list
                            SubsystemConfigList.Add(new MenuItemRtiSubsystem() { Header = result, IsCheckable = true, IsChecked = true, Command = SubsystemSelectionCommand, Subsystem = subsystem, CepoIndex = cepoIndex });
                        }
                    }
                }
            }
            catch (SQLiteException e)
            {
                Debug.WriteLine("Error using database to get file names", e);
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error using database to get file names", e);
                return;
            }
        }

        /// <summary>
        /// Get the query string to inclue the complete list of selected Subsystem.
        /// </summary>
        /// <returns></returns>
        protected string GenerateQuerySubsystemList()
        {
            if (SubsystemConfigList.Count <= 0)
            {
                return "";
            }

            // Build up the list of selected files
            StringBuilder sb = new StringBuilder();
            foreach (var item in SubsystemConfigList)
            {
                // If checked, add to list
                if (item.IsChecked)
                {
                    sb.Append(string.Format("(Subsystem = {0} AND CepoIndex = {1}) OR", item.Subsystem, item.CepoIndex));
                }
            }

            // Convert to string
            string subsystemList = sb.ToString();

            if (!string.IsNullOrEmpty(subsystemList))
            {
                // Remove the last AND
                subsystemList = subsystemList.Substring(0, subsystemList.Length - 3);
                return string.Format("AND ({0})", subsystemList);
            }

            return "";
        }

        #endregion

        #region Number of Ensembles

        /// <summary>
        /// Get the number of ensembles in the project database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <returns>Number of ensembles.</returns>
        protected int GetNumEnsembles(SQLiteConnection cnn)
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
        /// Get the number of ensembles in the project database.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="query">Query string for number of ensembles.</param>
        /// <returns>Number of ensembles.</returns>
        protected int GetNumEnsembles(SQLiteConnection cnn, string query)
        {
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

        #region Calculate Limit and Offset

        /// <summary>
        /// Calculate the limit and offset based off the indexes given.
        /// </summary>
        /// <param name="numEnsembles">Total number of ensembles in the project.</param>
        /// <param name="minIndex">Minimum index.</param>
        /// <param name="maxIndex">Maximum index.</param>
        /// <returns></returns>
        protected LimitOffset CalcLimitOffset(int numEnsembles, int minIndex, int maxIndex)
        {
            LimitOffset lo = new LimitOffset();

            // Verify max does not exceed the number of ensembles
            if (maxIndex > numEnsembles)
            {
                maxIndex = numEnsembles;
            }

            // Check less than 0
            if (maxIndex <= 0)
            {
                maxIndex = numEnsembles;
            }
            if (minIndex <= 0)
            {
                minIndex = 1;
            }

            // Verify min is less than max
            if (maxIndex < minIndex)
            {
                maxIndex = minIndex + 1;
            }

            // Verify min does not exceed max
            if (minIndex > maxIndex)
            {
                minIndex = maxIndex - 1;
            }

            // If min and max are used, set the limit and offset
            int limit = numEnsembles;               // Max number of ensembles to receive
            int offset = 0;                         // Start location in the project
            if (minIndex != 0 && maxIndex != 0)
            {
                limit = maxIndex - minIndex;        // Get the total number of ensembles selected
                offset = minIndex;                  // Get the start location
                numEnsembles = limit;               // Set the new number of ensembles selected

                // Verify limit is not to large
                if (limit > numEnsembles)
                {
                    limit = numEnsembles;
                }
            }


            lo.Limit = limit;
            lo.Offset = offset;

            return lo;
        }

        #endregion

        /// <summary>
        /// Replot the data when settings change.  
        /// Implement this method for each plot.
        /// </summary>
        /// <param name="eplotDataType">Plot type.</param>
        public virtual void ReplotData(PlotDataType eplotDataType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implement reploting the data.
        /// </summary>
        /// <param name="minIndex">Minimum Index.</param>
        /// <param name="maxIndex">Maximum Index.</param>
        public virtual void ReplotData(int minIndex, int maxIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implement replotting the data.
        /// </summary>
        public virtual void ReplotData()
        {
            throw new NotImplementedException();
        }
    }
}
