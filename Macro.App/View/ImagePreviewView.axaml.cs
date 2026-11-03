using Avalonia.Controls;
using Avalonia.Threading;
using Macro.Extensions;
using System.Drawing;
using PreviewBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Macro.View
{
    public class ImagePreviewView : UserControl
    {
        internal Avalonia.Controls.Image previewImage;

        private PreviewBitmap _previewBitmap;
        private bool _isDisposed;
        private Border _searchResultBorder;
        private TextBlock _searchResultText;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            previewImage = this.FindControl<Avalonia.Controls.Image>("previewImage");
            _searchResultBorder = this.FindControl<Border>("searchResultBorder");
            _searchResultText = this.FindControl<TextBlock>("searchResultText");
        }

        public ImagePreviewView()
        {
            InitializeComponent();
        }

        public void DrawImage(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                ClearImage();
                return;
            }
            PreviewBitmap previewBitmap = bitmap.ToBitmapSource();
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
                _searchResultBorder.IsVisible = false;
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
                _searchResultBorder.IsVisible = false;
            });
        }

        public void ShowSearchResult(Bitmap bitmap, string description)
        {
            DrawImage(bitmap);
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed == true)
                {
                    return;
                }
                _searchResultText.Text = description;
                _searchResultBorder.IsVisible = true;
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
