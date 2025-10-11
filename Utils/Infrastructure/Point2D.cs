using System.Drawing;
using System.Runtime.InteropServices;

namespace Utils.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InterSize
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point2D
    {
        public int X;
        public int Y;
        public Point2D(double x, double y)
        {
            X = (int)x;
            Y = (int)y;
        }
        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Point(Point2D point)
        {
            return new Point(point.X, point.Y);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct InterPointDouble
    {
        public double X;
        public double Y;

        public static implicit operator Point(InterPointDouble point)
        {
            return new Point((int)point.X, (int)point.Y);
        }
    }
}
