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
using System.Windows;
using System.Windows.Input;

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

        #region Sturct and Class

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

        /// <summary>
        /// File Entry in the project.
        /// </summary>
        public class FileEntry
        {
            /// <summary>
            /// Header name.
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// Set if this menu item is checkable.
            /// </summary>
            public bool IsCheckable { get; set; }

            /// <summary>
            /// Set if the menu is checked or not.
            /// </summary>
            public bool IsChecked { get; set; }

            /// <summary>
            /// Minimum date time.
            /// </summary>
            public DateTime MinDateTime { get; set; }

            /// <summary>
            /// Maximum date time.
            /// </summary>
            public DateTime MaxDateTime { get; set; }

            /// <summary>
            /// Number of ensembles.
            /// </summary>
            public int NumEnsembles { get; set; }

            /// <summary>
            /// Command.
            /// </summary>
            public ICommand Command { get; set; }

            /// <summary>
            /// Initialize value.
            /// </summary>
            public FileEntry()
            {
                FileName = "";
                MinDateTime = DateTime.Now;
                MaxDateTime = DateTime.Now;
                NumEnsembles = 0;
                IsCheckable = false;
                IsChecked = false;
            }
        }

        /// <summary>
        /// Subsystem Entry in the project.
        /// </summary>
        public class SubsystemEntry
        {
            /// <summary>
            /// Subsystem frequency.
            /// </summary>
            public string Subsystem { get; set; }

            /// <summary>
            /// CEPO index.
            /// </summary>
            public string CepoIndex { get; set; }

            /// <summary>
            /// Frequency description.
            /// </summary>
            public string Desc
            {
                get
                {
                    return GetDesc();
                }
            }

            /// <summary>
            /// Set if this menu item is checkable.
            /// </summary>
            public bool IsCheckable { get; set; }

            /// <summary>
            /// Set if the menu is checked or not.
            /// </summary>
            public bool IsChecked { get; set; }

            /// <summary>
            /// Number of ensembles.
            /// </summary>
            public int NumEnsembles { get; set; }

            /// <summary>
            /// Command.
            /// </summary>
            public ICommand Command { get; set; }

            /// <summary>
            /// Initialize value.
            /// </summary>
            public SubsystemEntry()
            {
                Subsystem = "";
                CepoIndex = "";
                NumEnsembles = 0;
                IsCheckable = false;
                IsChecked = false;
            }

            private string GetDesc()
            {
                string desc = "";

                switch(Subsystem)
                {
                    case "2":
                        return "1.2 MHz 4 beam 20 degree piston";
                    case "3":
                        return "600 kHz 4 beam 20 degree piston";
                    case "4":
                        return "300 kHz 4 beam 20 degree piston";
                    case "5":
                        return "2 MHz 4 beam 20 degree piston, 45 degree heading offset";
                    case "6":
                        return "1.2 MHz 4 beam 20 degree piston, 45 degree heading offset";
                    case "7":
                        return "600 kHz 4 beam 20 degree piston, 45 degree heading offset";
                    case "8":
                        return "300 kHz 4 beam 20 degree piston, 45 degree heading offset";
                    case "9":
                        return "2 MHz vertical beam piston";
                    case "A":
                        return "1.2 MHz vertical beam piston";
                    case "B":
                        return "600 kHz vertical beam piston";
                    case "C":
                        return "300 kHz vertical beam piston";
                    case "D":
                        return "150 kHz 4 beam 20 degree piston";
                    case "E":
                        return "75 kHz 4 beam 20 degree piston";
                    case "F":
                        return "38 kHz 4 beam 20 degree piston";
                    case "G":
                        return "20 kHz 4 beam 20 degree piston";
                }


                return desc;
            }
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
        public BindingList<FileEntry> ProjectFileList { get; set; }

        /// <summary>
        /// List of all the Subsystem configurations.
        /// </summary>
        public BindingList<SubsystemEntry> SubsystemConfigList { get; set; }

        #endregion

        #region Filter Data

        /// <summary>
        /// Filter the data for bad values.
        /// </summary>
        protected bool _IsFilterData;
        /// <summary>
        /// Filter the data for bad values.
        /// </summary>
        public bool IsFilterData
        {
            get { return _IsFilterData; }
            set
            {
                _IsFilterData = value;
                NotifyOfPropertyChange(() => IsFilterData);
            }
        }

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
            IsFilterData = true;

            // List of project files and subsystem
            ProjectFileList = new BindingList<FileEntry>();
            SubsystemConfigList = new BindingList<SubsystemEntry>();

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
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ProjectFileList.Clear();
            });

            GetFileList(fileName);

            // Clear the current list and get the new subsystem configurations list from the project
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                SubsystemConfigList.Clear();
            });


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

                            // Create a file entry
                            FileEntry fe = new FileEntry() { FileName = reader["FileName"].ToString(), IsCheckable = true, IsChecked = true, Command = FileSelectionCommand };

                            // Get the number of ensembles for the file
                            fe.NumEnsembles = GetNumEnsemblesPerFile(cnn, fe.FileName);

                            // Get the first and last date
                            fe.MinDateTime = GetFirstDateTimePerFile(cnn, fe.FileName);
                            fe.MaxDateTime = GetLastDateTimePerFile(cnn, fe.FileName);

                            // Add file name to list
                            ProjectFileList.Add(fe);
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
        /// Get the number of ensemble in the file.
        /// </summary>
        /// <param name="cnn">SQLite connection.</param>
        /// <param name="fileName">File name to get the number of ensembles.</param>
        /// <returns>Number of ensembles in the file.</returns>
        protected int GetNumEnsemblesPerFile(SQLiteConnection cnn, string fileName)
        {
            // Init value
            int count = 0; 

            // Create a command to query
            using (DbCommand cmd = cnn.CreateCommand())
            {
                string query = string.Format("SELECT COUNT(*) FROM {0} WHERE FileName='{1}';", "tblEnsemble", fileName);
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                count = Convert.ToInt32(resultValue.ToString());
            }

            return count;
        }

        protected DateTime GetFirstDateTimePerFile(SQLiteConnection cnn, string fileName)
        {
            // Init value
            DateTime dt = DateTime.Now;

            // Create a command to query
            using (DbCommand cmd = cnn.CreateCommand())
            {
                string query = string.Format("SELECT DateTime FROM {0} WHERE FileName='{1}' LIMIT 1;", "tblEnsemble", fileName);
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                dt = Convert.ToDateTime(resultValue.ToString());
            }

            return dt;
        }

        protected DateTime GetLastDateTimePerFile(SQLiteConnection cnn, string fileName)
        {
            // Init value
            DateTime dt = DateTime.Now;

            // Create a command to query
            using (DbCommand cmd = cnn.CreateCommand())
            {
                string query = string.Format("SELECT DateTime FROM {0} WHERE FileName='{1}' ORDER BY DateTime DESC LIMIT 1;", "tblEnsemble", fileName);       // Descending
                cmd.CommandText = query;

                // Get Result
                object resultValue = cmd.ExecuteScalar();
                dt = Convert.ToDateTime(resultValue.ToString());
            }

            return dt;
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
                    sb.Append(string.Format("'{0}',", item.FileName));
                }
            }

            // Convert to string
            string fileList = sb.ToString();

            if (!string.IsNullOrEmpty(fileList))
            {
                // Remove the last comma
                fileList = fileList.Substring(0, fileList.Length - 1);

                // After removing the comma, check if it is empty
                if (!string.IsNullOrEmpty(fileList) && fileList.Length > 3)
                {
                    return string.Format("AND (FileName IN ({0}))", fileList);
                }
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
                            SubsystemConfigList.Add(new SubsystemEntry() { IsCheckable = true, IsChecked = true, Command = SubsystemSelectionCommand, Subsystem = subsystem, CepoIndex = cepoIndex });
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
                // If checked, add to lists
                if (item.IsChecked && !string.IsNullOrEmpty(item.Subsystem.TrimEnd('\0')))
                {
                    sb.Append(string.Format("(Subsystem = \"{0}\" AND CepoIndex = {1}) OR", item.Subsystem, item.CepoIndex));
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
