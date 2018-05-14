using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{

    /// <summary>
    /// Different Plot types.
    /// </summary>
    public enum PlotDataType
    {
        Magnitude,
        Direction,
        Amplitude,
    }

    /// <summary>
    /// Plot View Model interface.
    /// </summary>
    public interface IPlotViewModel
    {
        /// <summary>
        /// Load the project to the plot.
        /// </summary>
        /// <param name="filePath">File Path to the project file.</param>
        /// <param name="minIndex">Minimum ensemble index to display.</param>
        /// <param name="maxIndex">Minimum ensemble index to display.</param>
        void LoadProject(string fileName, int minIndex = 0, int maxIndex = 0);

        /// <summary>
        /// Update the min and max ensembles selected.
        /// </summary>
        /// <param name="minIndex">Minimum ensemble index.</param>
        /// <param name="maxIndex">Maximum ensemble index.</param>
        void ReplotData(int minIndex, int maxIndex);

        /// <summary>
        /// Update plot.
        /// </summary>
        void ReplotData();

        /// <summary>
        /// Replot the data based off the selected type.
        /// </summary>
        /// <param name="eplotDataType">Selected Plot type.</param>
        void ReplotData(PlotDataType eplotDataType);
    }
}
