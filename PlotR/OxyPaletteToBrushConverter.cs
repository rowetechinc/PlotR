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
 * Date            Initials    Comments
 * -----------------------------------------------------------------
 * 04/01/2015      RC          Initial coding
 * 
 */


using System.Windows.Data;
using System.Windows.Media;
using System;
using OxyPlot;
using System.Windows.Shapes;
using System.Windows;

namespace PlotR
{
    /// <summary>
    /// Convert the given OxyPalette to a LinearGradientBrush.
    /// This is used to display colors in a combobox.
    /// </summary>
    [ValueConversion(typeof(OxyPalette), typeof(Brush))]
    public class OxyPaletteToBrushConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Convert the color to a brush.
        /// </summary>
        /// <param name="value">OxyPalette.</param>
        /// <param name="targetType">Brush type.</param>
        /// <param name="parameter">.</param>
        /// <param name="culture">.</param>
        /// <returns>SolidColorBrush of the color.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is OxyPalette)) return null;

            // Get the number of colors in the palette
            int count = ((OxyPalette)value).Colors.Count;
            double offset = 1;
            if (count > 0)
            {
                offset = 1.0 / count;
            }

            // Create a vertical linear gradient with the number of stops based off the number of colors in the palette  
            double index = 0.0;
            LinearGradientBrush myVerticalGradient = new LinearGradientBrush();
            myVerticalGradient.StartPoint = new Point(0.0, 00.5);
            myVerticalGradient.EndPoint = new Point(1, 0.5);
            foreach (var color in ((OxyPalette)value).Colors)
            {
                myVerticalGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(color.ToString()), index));
                index += offset;
            }

            return myVerticalGradient;
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