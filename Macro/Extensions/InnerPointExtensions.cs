using System.Windows;
using Utils.Infrastructure;

namespace Utils.Extensions
{
    public static class InnerPointExtensions
    {
        public static int ToLParam(this Point2D point) => (int)point.X & 0xFFFF | ((int)point.Y << 0x10);


        public static Vector Subtract(this Point2D point1, Point2D point2)
        {
            return new Vector(point1.X - point2.X, point1.Y - point2.Y);
        }
    }
}
