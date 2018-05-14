using System.Windows.Data;
using System.Windows.Media;
using System;
using OxyPlot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotR
{
    /// <summary>
    /// Convert the given OxyColor to a SolidColorBrush.
    /// This is used to display colors in a combobox.
    /// </summary>
    [ValueConversion(typeof(OxyColor), typeof(Brush))]
    public class OxyColorToBrushConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Convert the color to a brush.
        /// </summary>
        /// <param name="value">OxyColor.</param>
        /// <param name="targetType">Brush type.</param>
        /// <param name="parameter">.</param>
        /// <param name="culture">.</param>
        /// <returns>SolidColorBrush of the color.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is OxyColor)) return null;
            SolidColorBrush scb = new SolidColorBrush((Color)ColorConverter.ConvertFromString(((OxyColor)value).ToString()));
            return scb;
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <param name="value">.</param>
        /// <param name="targetType">.</param>
        /// <param name="parameter">.</param>
        /// <param name="culture">.</param>
        /// <returns>Not implemented.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
