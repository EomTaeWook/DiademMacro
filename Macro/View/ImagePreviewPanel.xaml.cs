using MahApps.Metro.Controls;
using OpenCvSharp.WpfExtensions;
using System.Drawing;
using System.Windows.Media;

namespace Macro.View
{
    /// <summary>
    /// ProcessCaptureDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImagePreviewPanel : MetroWindow
    {
        public ImagePreviewPanel()
        {
            InitializeComponent();
            this.Loaded += ImagePreviewPanel_Loaded;
        }

        private void ImagePreviewPanel_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Left = this.Owner.Left - this.Width;
            this.Top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;
        }

        public void DrawImage(Bitmap bmp)
        {
            Dispatcher.Invoke(() =>
            {
                canvasCaptureImage.Background = new ImageBrush(bmp.ToBitmapSource());
            });
        }
        public void ClearImage()
        {
            Dispatcher.Invoke(() =>
            {
                canvasCaptureImage.Background = new SolidColorBrush(Colors.White);
            });
        }
    }
}
