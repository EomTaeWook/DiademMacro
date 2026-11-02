using System.Drawing;
#if WINDOWS
using System.Drawing.Imaging;
#endif
using System.IO;

namespace Macro.Extensions
{
    public static class ImageExtensions
    {
        public static Avalonia.Media.Imaging.Bitmap ToBitmapSource(this Bitmap source)
        {
            using MemoryStream stream = new();
#if WINDOWS
            source.Save(stream, ImageFormat.Png);
#else
            source.Save(stream);
#endif
            stream.Position = 0;
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }
}
