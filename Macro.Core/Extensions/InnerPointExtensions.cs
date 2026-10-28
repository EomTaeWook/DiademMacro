using System.Numerics;
using Utils.Infrastructure;

namespace Utils.Extensions
{
    public static class InnerPointExtensions
    {
        public static int ToLParam(this Point2D point)
        {
            return (int)point.X & 0xFFFF | ((int)point.Y << 0x10);
        }

        public static Vector2 Subtract(this Point2D firstPoint, Point2D secondPoint)
        {
            return new Vector2(firstPoint.X - secondPoint.X, firstPoint.Y - secondPoint.Y);
        }
    }
}
