using System;
using System.Runtime.InteropServices;

namespace Utils.Infrastructure
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IEquatable<Rect>
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Width { get => Right - Left; }
        public int Height { get => Bottom - Top; }

        public static Rect operator -(Rect r1, Rect r2)
        {
            return new Rect()
            {
                Left = r1.Left - r2.Left,
                Right = r1.Right - r2.Right,
                Bottom = r1.Bottom - r2.Bottom,
                Top = r1.Top - r2.Top
            };
        }
        public static Rect operator +(Rect r1, Rect r2)
        {
            return new Rect()
            {
                Left = r1.Left + r2.Left,
                Right = r1.Right + r2.Right,
                Bottom = r1.Bottom + r2.Bottom,
                Top = r1.Top + r2.Top
            };
        }

        public bool Equals(Rect other)
        {
            return object.Equals(this, other);
        }
    }

    [Serializable]
    public class Rectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Width { get => Right - Left; }
        public int Height { get => Bottom - Top; }
        public static implicit operator Rectangle(Rect rect)
        {
            return new Rectangle()
            {
                Left = rect.Left,
                Top = rect.Top,
                Bottom = rect.Bottom,
                Right = rect.Right
            };
        }
        public static Rectangle operator -(Rectangle r1, Rectangle r2)
        {
            return new Rectangle()
            {
                Left = r1.Left - r2.Left,
                Right = r1.Right - r2.Right,
                Bottom = r1.Bottom - r2.Bottom,
                Top = r1.Top - r2.Top
            };
        }
        public static Rectangle operator +(Rectangle r1, Rectangle r2)
        {
            return new Rectangle()
            {
                Left = r1.Left + r2.Left,
                Right = r1.Right + r2.Right,
                Bottom = r1.Bottom + r2.Bottom,
                Top = r1.Top + r2.Top
            };
        }
    }

}
