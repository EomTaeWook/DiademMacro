using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Collections.Generic;
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

    }
}
