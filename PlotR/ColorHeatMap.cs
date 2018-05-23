using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;// for WPF
                           // for WindowsForms using System.Drawing


namespace PlotR
{
    /// <summary>
    /// https://stackoverflow.com/questions/17821828/calculating-heat-map-colours?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
    /// 
    /// Convert the value to a color based off the color map.
    /// </summary>
    public class ColorHeatMap
    {
        /// <summary>
        /// Default alpha value.
        /// </summary>
        public byte Alpha = 0xff;

        /// <summary>
        /// Color map.
        /// </summary>
        public List<Color> ColorsOfMap = new List<Color>();

        /// <summary>
        /// Init the color map.
        /// </summary>
        public ColorHeatMap()
        {
            initColorsBlocks();
        }

        /// <summary>
        /// Init the color map with an alpha value.
        /// </summary>
        /// <param name="alpha"></param>
        public ColorHeatMap(byte alpha)
        {
            this.Alpha = alpha;
            initColorsBlocks();
        }

        /// <summary>
        /// Initialize the color blocks.
        /// </summary>
        private void initColorsBlocks()
        {
            ColorsOfMap.AddRange(new Color[]{
            Color.FromArgb(Alpha, 0, 0, 0) ,            //Black
            Color.FromArgb(Alpha, 0, 0, 0xFF) ,         //Blue
            Color.FromArgb(Alpha, 0, 0xFF, 0xFF) ,      //Cyan
            Color.FromArgb(Alpha, 0, 0xFF, 0) ,         //Green
            Color.FromArgb(Alpha, 0xFF, 0xFF, 0) ,      //Yellow
            Color.FromArgb(Alpha, 0xFF, 0, 0) ,         //Red
            Color.FromArgb(Alpha, 0xFF, 0xFF, 0xFF)     // White
        });
        }

        /// <summary>
        /// Convert the Value to a color based off a color map.
        /// </summary>
        /// <param name="val">Value.</param>
        /// <param name="minVal">Minimum value.</param>
        /// <param name="maxVal">Maximum value.</param>
        /// <returns>Color value.</returns>
        public Color GetColorForValue(double val, double minVal, double maxVal)
        {
            Color c = ColorsOfMap[0];

            try
            {
                double valPerc = 0.0;
                if ((maxVal - minVal) != 0)
                {
                    valPerc = (val - minVal) / (maxVal - minVal);               // value %
                }
                double colorPerc = 1d / (ColorsOfMap.Count - 1);                // % of each block of color. the last is the "100% Color"
                double blockOfColor = valPerc / colorPerc;                      // the integer part repersents how many block to skip
                int blockIdx = (int)Math.Truncate(blockOfColor);                // Idx of 
                double valPercResidual = valPerc - (blockIdx * colorPerc);      //remove the part represented of block 
                double percOfColor = valPercResidual / colorPerc;               // % of color of this block that will be filled

                Color cTarget = ColorsOfMap[ColorsOfMap.Count - 1]; 
                if (blockIdx < ColorsOfMap.Count)
                {
                    cTarget = ColorsOfMap[blockIdx];
                }

                Color cNext = ColorsOfMap[ColorsOfMap.Count - 1];
                if (val < maxVal)
                {
                    cNext = val == maxVal ? ColorsOfMap[blockIdx] : ColorsOfMap[blockIdx + 1];
                }

                var deltaR = cNext.R - cTarget.R;
                var deltaG = cNext.G - cTarget.G;
                var deltaB = cNext.B - cTarget.B;

                var R = cTarget.R + (deltaR * percOfColor);
                var G = cTarget.G + (deltaG * percOfColor);
                var B = cTarget.B + (deltaB * percOfColor);

                c = Color.FromArgb(Alpha, (byte)R, (byte)G, (byte)B);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error converting color.", ex);
                return c;
            }

            return c;
        }

        /// <summary>
        /// Create the color map legend on a canvas.  The canvas 320 in height and 40 in width (320x40).
        /// 
        /// Binding
        /// [ContentPresenter Content="{Binding ColorMapCanvas}" Width="40" Margin="5,5,20,5" HorizontalAlignment="Right"/]
        /// </summary>
        /// <returns>Canvas of the color map legend.</returns>
        public System.Windows.Controls.Canvas GetColorMapLegend(double minVal, double maxVal)
        {
            // Width and height
            double width = 20;
            double height = 5;
            int index = 0;
            int STEPS = 64;

            // Create a canvas
            System.Windows.Controls.Canvas canvas = new System.Windows.Controls.Canvas();

            // All all the colors
            double step = ((maxVal - minVal) / STEPS);
            for (double x = minVal; x < maxVal; x += step)
            {
                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
                rectangle.Width = width;
                rectangle.Height = height;
                //rectangle.Fill = new System.Windows.Media.SolidColorBrush(color);
                rectangle.Fill = new System.Windows.Media.SolidColorBrush(GetColorForValue(x, minVal, maxVal));

                // Set the position
                System.Windows.Controls.Canvas.SetLeft(rectangle, 0);                       // Set to 0 to stack
                System.Windows.Controls.Canvas.SetTop(rectangle, (height * index++));       // Stack each rectangle

                // Add it to the canvas
                canvas.Children.Add(rectangle);
            }

            // Add Text
            System.Windows.Controls.TextBlock text = new System.Windows.Controls.TextBlock();
            text.Text = minVal.ToString("0.0");                                         // Set the text
            System.Windows.Controls.Canvas.SetLeft(text, width + 5.0);                  // Move next to rectangle
            System.Windows.Controls.Canvas.SetTop(text, 0);                             // Stack each rectangle
            canvas.Children.Add(text);                                                  // Add it to the canvas

            // Add Text
            double topMiddleVal = ((maxVal - minVal) * 0.75);
            double topMiddleLoc = ((height * STEPS) * 0.75);
            System.Windows.Controls.TextBlock textTopMiddle = new System.Windows.Controls.TextBlock();
            textTopMiddle.Text = topMiddleVal.ToString("0.0");                                                  // Set the text
            System.Windows.Controls.Canvas.SetLeft(textTopMiddle, width + 5.0);                                 // Move next to rectangle
            System.Windows.Controls.Canvas.SetTop(textTopMiddle, topMiddleLoc - (height / 2.0));                // Top Middle
            canvas.Children.Add(textTopMiddle);

            // Add Text
            double middleVal = ((maxVal - minVal) * 0.5);
            double middleLoc = ((height * STEPS) * 0.5);
            System.Windows.Controls.TextBlock textMiddle = new System.Windows.Controls.TextBlock();
            textMiddle.Text = middleVal.ToString("0.0");                                                        // Set the text
            System.Windows.Controls.Canvas.SetLeft(textMiddle, width + 5.0);                                    // Move next to rectangle
            System.Windows.Controls.Canvas.SetTop(textMiddle, middleLoc - (height / 2.0));                      // Top Middle
            canvas.Children.Add(textMiddle);

            // Add Text
            double bottomMiddleVal = ((maxVal - minVal) * 0.25);
            double bottomMiddleLoc = ((height * STEPS) * 0.25);
            System.Windows.Controls.TextBlock textBottomMiddle = new System.Windows.Controls.TextBlock();
            textBottomMiddle.Text = bottomMiddleVal.ToString("0.0");                                            // Set the text
            System.Windows.Controls.Canvas.SetLeft(textBottomMiddle, width + 5.0);                              // Move next to rectangle
            System.Windows.Controls.Canvas.SetTop(textBottomMiddle, bottomMiddleLoc - (height / 2.0));          // Top Middle
            canvas.Children.Add(textBottomMiddle);

            // Add Text
            System.Windows.Controls.TextBlock textBottom = new System.Windows.Controls.TextBlock();
            textBottom.Text = maxVal.ToString("0.0");                                                           // Set the text
            System.Windows.Controls.Canvas.SetLeft(textBottom, width + 5.0);                                    // Move next to rectangle
            System.Windows.Controls.Canvas.SetTop(textBottom, (height * STEPS) - (height/2.0));                 // Set to the bottom
            canvas.Children.Add(textBottom);

            return canvas;
        }

    }
}
