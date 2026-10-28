using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Macro.Extensions
{
    public static class ImageExtensions
    {
        public static Avalonia.Media.Imaging.Bitmap ToBitmapSource(this Bitmap source)
        {
            using MemoryStream stream = new();
            source.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }
}
