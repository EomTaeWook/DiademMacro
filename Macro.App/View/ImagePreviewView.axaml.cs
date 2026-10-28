using Avalonia.Controls;
using Avalonia.Threading;
using Macro.Extensions;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Macro.View
{
    public class ImagePreviewView : UserControl
    {
        internal Avalonia.Controls.Image previewImage;

        private Bitmap _previewBitmap;
        private bool _isDisposed;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            previewImage = this.FindControl<Avalonia.Controls.Image>("previewImage");
        }

        public ImagePreviewView()
        {
            InitializeComponent();
        }

        public void DrawImage(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
            {
                ClearImage();
                return;
            }
            Bitmap previewBitmap = bitmap.ToBitmapSource();
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed == true)
                {
                    previewBitmap.Dispose();
                    return;
                }
                previewImage.Source = previewBitmap;
                _previewBitmap?.Dispose();
                _previewBitmap = previewBitmap;
            });
        }

        public void ClearImage()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed == true)
                {
                    return;
                }
                previewImage.Source = null;
                _previewBitmap?.Dispose();
                _previewBitmap = null;
            });
        }

        public void Dispose()
        {
            _isDisposed = true;
            previewImage.Source = null;
            _previewBitmap?.Dispose();
            _previewBitmap = null;
        }
    }
}
