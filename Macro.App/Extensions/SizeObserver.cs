using Avalonia;
using Avalonia.Controls;

namespace Macro.Extensions
{
    public class SizeObserver : AvaloniaObject
    {
        public static readonly AttachedProperty<bool> ObserveProperty = AvaloniaProperty.RegisterAttached<SizeObserver, Control, bool>("Observe");
        public static readonly AttachedProperty<double> ObservedWidthProperty = AvaloniaProperty.RegisterAttached<SizeObserver, Control, double>("ObservedWidth");
        public static readonly AttachedProperty<double> ObservedHeightProperty = AvaloniaProperty.RegisterAttached<SizeObserver, Control, double>("ObservedHeight");

        static SizeObserver()
        {
            ObserveProperty.Changed.AddClassHandler<Control>(OnObserveChanged);
        }

        public static bool GetObserve(Control control)
        {
            return control.GetValue(ObserveProperty);
        }

        public static void SetObserve(Control control, bool observe)
        {
            control.SetValue(ObserveProperty, observe);
        }

        public static double GetObservedWidth(Control control)
        {
            return control.GetValue(ObservedWidthProperty);
        }

        public static void SetObservedWidth(Control control, double width)
        {
            control.SetValue(ObservedWidthProperty, width);
        }

        public static double GetObservedHeight(Control control)
        {
            return control.GetValue(ObservedHeightProperty);
        }

        public static void SetObservedHeight(Control control, double height)
        {
            control.SetValue(ObservedHeightProperty, height);
        }

        private static void OnObserveChanged(Control control, AvaloniaPropertyChangedEventArgs eventArgs)
        {
            if (GetObserve(control) == true)
            {
                control.SizeChanged += OnSizeChanged;
                UpdateObservedSizes(control);
            }
            else
            {
                control.SizeChanged -= OnSizeChanged;
            }
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            UpdateObservedSizes((Control)sender);
        }

        private static void UpdateObservedSizes(Control control)
        {
            control.SetCurrentValue(ObservedWidthProperty, control.Bounds.Width);
            control.SetCurrentValue(ObservedHeightProperty, control.Bounds.Height);
        }
    }
}
