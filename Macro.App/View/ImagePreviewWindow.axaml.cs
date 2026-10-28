using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Macro.Extensions;
using System;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Macro.View
{
    public class ImagePreviewWindow : Window
    {
        internal Avalonia.Controls.Canvas canvasCaptureImage;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            canvasCaptureImage = this.FindControl<Avalonia.Controls.Canvas>("canvasCaptureImage");
        }

        private Bitmap _image;
        private bool _isClosed;

        public ImagePreviewWindow()
        {
            InitializeComponent();
            Opened += ImagePreviewWindow_Opened;
            Closed += ImagePreviewWindow_Closed;
        }

        private void ImagePreviewWindow_Opened(object sender, EventArgs eventArgs)
        {
            if (Owner is Window owner)
            {
                PixelPoint ownerPosition = owner.Position;
                Position = new PixelPoint(ownerPosition.X - (int)(Width * RenderScaling), ownerPosition.Y + (int)((owner.Height - Height) * RenderScaling / 2));
            }
        }

        public void DrawImage(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
            {
                ClearImage();
                return;
            }
            Bitmap image = bitmap.ToBitmapSource();
            Dispatcher.UIThread.Post(() =>
            {
                if (_isClosed == true)
                {
                    image.Dispose();
                    return;
                }
                canvasCaptureImage.Background = new ImageBrush(image) { Stretch = Stretch.Uniform };
                _image?.Dispose();
                _image = image;
            });
        }

        public void ClearImage()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isClosed == true)
                {
                    return;
                }
                canvasCaptureImage.Background = Brushes.White;
                _image?.Dispose();
                _image = null;
            });
        }

        private void ImagePreviewWindow_Closed(object sender, EventArgs eventArgs)
        {
            _isClosed = true;
            _image?.Dispose();
            _image = null;
        }
    }
}
