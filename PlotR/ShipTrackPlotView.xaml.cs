using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

namespace PlotR
{
    /// <summary>
    /// Interaction logic for ShipTrackPlotView.xaml
    /// </summary>
    public partial class ShipTrackPlotView : UserControl
    {
        public ShipTrackPlotView()
        {
            InitializeComponent();
        }

        public void TakeScreenShot()
        {

            string path = System.IO.Path.GetTempPath() + System.IO.Path.GetRandomFileName() + @".png";
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                BitmapImage _tmpImage = MapView.ToImageSource() as BitmapImage;
                if (_tmpImage == null) return;

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_tmpImage.StreamSource));
                encoder.Save(fileStream);

                Debug.WriteLine("Save to Path: {0}", path);
            }
        }

    }
}
