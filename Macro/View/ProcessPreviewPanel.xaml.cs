using MahApps.Metro.Controls;
using OpenCvSharp.WpfExtensions;
using System.Drawing;
using System.Windows.Media;

namespace Macro.View
{
    /// <summary>
    /// ProcessCaptureDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProcessPreviewPanel : MetroWindow
    {
        public ProcessPreviewPanel()
        {
            InitializeComponent();
        }

        public void DrawCaptureImage(Bitmap bmp)
        {
            Dispatcher.Invoke(() =>
            {
                canvasCaptureImage.Background = new ImageBrush(bmp.ToBitmapSource());
            });
        }
    }
}
