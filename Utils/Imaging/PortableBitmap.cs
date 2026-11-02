using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using Size = System.Drawing.Size;

namespace Utils.Imaging
{
    [Serializable]
    public class PortableBitmap : IDisposable, ICloneable, ISerializable
    {
        private byte[] _pixels;
        public int Width { get; }
        public int Height { get; }
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
        }

        public PortableBitmap(int width, int height)
        {
            long length = (long)width * height * 4;
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            if (length > Array.MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            Width = width;
            Height = height;
            _pixels = new byte[(int)length];
        }

        public PortableBitmap(Stream stream)
        {
            using AvaloniaBitmap decoded = new(stream);
            Width = decoded.PixelSize.Width;
            Height = decoded.PixelSize.Height;
            long length = (long)Width * Height * 4;
            if (length > Array.MaxLength)
            {
                throw new InvalidDataException("The image exceeds the supported size.");
            }
            _pixels = new byte[(int)length];
            using WriteableBitmap normalized = new(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using ILockedFramebuffer framebuffer = normalized.Lock();
            decoded.CopyPixels(framebuffer);
            for (int row = 0; row < Height; row++)
            {
                Marshal.Copy(IntPtr.Add(framebuffer.Address, row * framebuffer.RowBytes), _pixels, row * Width * 4, Width * 4);
            }
        }

        public PortableBitmap(string path) : this(new MemoryStream(File.ReadAllBytes(path), false))
        {
        }

        public PortableBitmap(PortableBitmap source) : this(source, source.Width, source.Height)
        {
        }

        public PortableBitmap(PortableBitmap source, int width, int height) : this(width, height)
        {
            source.EnsureNotDisposed();
            if (source.Width == width)
            {
                if (source.Height == height)
                {
                    source._pixels.CopyTo(_pixels, 0);
                    return;
                }
            }
            for (int row = 0; row < height; row++)
            {
                int sourceRow = (int)((long)row * source.Height / height);
                for (int column = 0; column < width; column++)
                {
                    int sourceColumn = (int)((long)column * source.Width / width);
                    Buffer.BlockCopy(source._pixels, (sourceRow * source.Width + sourceColumn) * 4, _pixels, (row * width + column) * 4, 4);
                }
            }
        }

        protected PortableBitmap(SerializationInfo information, StreamingContext context) : this(new MemoryStream((byte[])information.GetValue("Data", typeof(byte[])), false))
        {
        }

        public void GetObjectData(SerializationInfo information, StreamingContext context)
        {
            using MemoryStream stream = new();
            Save(stream);
            information.AddValue("Data", stream.ToArray());
        }

        public ReadOnlySpan<byte> GetPixels()
        {
            EnsureNotDisposed();
            return _pixels;
        }

        public object Clone()
        {
            return new PortableBitmap(this);
        }

        public PortableBitmap Clone(Rectangle rectangle)
        {
            EnsureNotDisposed();
            if (rectangle.Left < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rectangle));
            }
            if (rectangle.Top < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rectangle));
            }
            if ((long)rectangle.Left + rectangle.Width > Width)
            {
                throw new ArgumentOutOfRangeException(nameof(rectangle));
            }
            if ((long)rectangle.Top + rectangle.Height > Height)
            {
                throw new ArgumentOutOfRangeException(nameof(rectangle));
            }
            PortableBitmap result = new(rectangle.Width, rectangle.Height);
            for (int row = 0; row < rectangle.Height; row++)
            {
                Buffer.BlockCopy(_pixels, ((rectangle.Top + row) * Width + rectangle.Left) * 4, result._pixels, row * rectangle.Width * 4, rectangle.Width * 4);
            }
            return result;
        }

        public void Save(Stream stream)
        {
            EnsureNotDisposed();
            using WriteableBitmap bitmap = new(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using (ILockedFramebuffer framebuffer = bitmap.Lock())
            {
                for (int row = 0; row < Height; row++)
                {
                    Marshal.Copy(_pixels, row * Width * 4, IntPtr.Add(framebuffer.Address, row * framebuffer.RowBytes), Width * 4);
                }
            }
            bitmap.Save(stream);
        }

        public void FillRectangle(Rectangle rectangle, Color color)
        {
            EnsureNotDisposed();
            int right = Math.Min(Width, rectangle.Right);
            int bottom = Math.Min(Height, rectangle.Bottom);
            for (int row = Math.Max(0, rectangle.Top); row < bottom; row++)
            {
                for (int column = Math.Max(0, rectangle.Left); column < right; column++)
                {
                    int offset = (row * Width + column) * 4;
                    _pixels[offset] = color.B;
                    _pixels[offset + 1] = color.G;
                    _pixels[offset + 2] = color.R;
                    _pixels[offset + 3] = color.A;
                }
            }
        }

        public void DrawRectangle(Rectangle rectangle, Color color, int thickness = 1)
        {
            FillRectangle(new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
            FillRectangle(new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            FillRectangle(new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
            FillRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_pixels == null, this);
        }

        public void Dispose()
        {
            _pixels = null;
        }
    }
}
