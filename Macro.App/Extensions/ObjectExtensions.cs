using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using Point = Avalonia.Point;

namespace Macro.Extensions
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> FindChildren<T>(this Control source) where T : Control
        {
            if (source == null)
            {
                return Enumerable.Empty<T>();
            }
            return source.GetLogicalDescendants().OfType<T>();
        }

        public static Border Clone(this Border source)
        {
            return new Border
            {
                BorderBrush = source.BorderBrush,
                BorderThickness = source.BorderThickness,
                Background = source.Background,
                Opacity = source.Opacity,
                CornerRadius = source.CornerRadius,
                UseLayoutRounding = source.UseLayoutRounding
            };
        }

        public static int MakeWParam(int low, int high)
        {
            return (low & 0xFFFF) | (high << 16);
        }

        public static Bitmap Resize(this Bitmap source, int width, int height)
        {
            Bitmap bitmap = new(width, height);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawImage(source, 0, 0, width, height);
            return bitmap;
        }

        public static T TryFindFromPoint<T>(this Control element, Point point) where T : class
        {
            Visual visual = element.GetVisualAt(point);
            if (visual is T result)
            {
                return result;
            }
            if (visual == null)
            {
                return null;
            }
            return visual.GetVisualAncestors().OfType<T>().FirstOrDefault();
        }

        public static void Swap<T>(this ObservableCollection<T> collection, int firstIndex, int secondIndex)
        {
            T item = collection[firstIndex];
            collection[firstIndex] = collection[secondIndex];
            collection[secondIndex] = item;
        }

        public static T GetSelectItemFromObject<T>(this ItemsControl control, object item) where T : ItemsControl
        {
            if (control.ContainerFromItem(item) is T target)
            {
                return target;
            }
            for (int index = 0; index < control.Items.Count; index++)
            {
                if (control.ContainerFromIndex(index) is T child)
                {
                    T descendant = GetSelectItemFromObject<T>(child, item);
                    if (descendant != null)
                    {
                        return descendant;
                    }
                }
            }
            return null;
        }

        public static T DataContext<T>(this StyledElement control) where T : class
        {
            return control.DataContext as T;
        }

        public static T FindVisualParent<T>(Visual visual) where T : Visual
        {
            if (visual == null)
            {
                return null;
            }
            return visual.GetVisualAncestors().OfType<T>().FirstOrDefault();
        }
    }
}
