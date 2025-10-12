using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Macro.UI
{
    public class TreeGridView : TreeView
    {
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(GridViewColumnCollection), typeof(TreeGridView), new UIPropertyMetadata(null));

        static TreeGridView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeGridView), new FrameworkPropertyMetadata(typeof(TreeGridView)));
        }
        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }
        public TreeGridView()
        {
            Columns = new GridViewColumnCollection();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) == false)
            {
                Application.Current.MainWindow.SizeChanged += MainWindow_SizeChanged;
            }
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var column in Columns)
            {
                BindingOperations.GetBindingExpression(column, WidthProperty).UpdateTarget();
            }
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeGridViewItem();
        }
    }

    public class TreeGridViewItem : TreeViewItem
    {
        public TreeViewItem ParentItem { get; set; }
        internal bool IsDrag { get; set; }
        public TreeGridViewItem()
        {
            IsDrag = false;
            DragEnter += TreeGridViewItem_DragEnter;
            DragLeave += TreeGridViewItem_DragLeave;
            Drop += TreeGridViewItem_Drop;
            MouseLeave += TreeGridViewItem_MouseLeave;
        }

        private void TreeGridViewItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is TreeGridViewItem item && item.IsDrag)
            {
                UpdateLeave(item);
            }
        }

        private void TreeGridViewItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TreeGridViewItem item && item.IsDrag)
            {
                UpdateLeave(item);
            }
        }

        private void TreeGridViewItem_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is TreeGridViewItem item && item.IsDrag)
            {
                UpdateLeave(item);
            }
        }

        private void TreeGridViewItem_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is TreeGridViewItem item && !item.IsDrag)
            {
                item.IsDrag = true;
                if (item.Template.FindName("Border", item) is Border border)
                {
                    border.BorderThickness = new Thickness(0, 0, 0, 2);
                    border.BorderBrush = Brushes.Red;
                }
                if (item.Template.FindName("StackPanel", item) is StackPanel panel)
                {
                    panel.Background = Brushes.PowderBlue;
                }
            }
        }
        private void UpdateLeave(TreeGridViewItem item)
        {
            if (item == null)
            {
                return;
            }

            item.IsDrag = false;

            if (item.Template.FindName("Border", item) is Border border)
            {
                border.BorderThickness = new Thickness(0, 0, 0, 0);
                border.BorderBrush = Brushes.Transparent;
            }
            if (item.Template.FindName("StackPanel", item) is StackPanel panel)
            {
                panel.Background = Brushes.Transparent;
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (!(element is TreeGridViewItem treeViewItem))
            {
                return;
            }

            treeViewItem.ParentItem = ItemsControlFromItemContainer(element) as TreeGridViewItem;
        }
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeGridViewItem();
        }
    }

    internal class TreeGridViewExpander : ToggleButton
    {
    }
    internal class TreeGridViewConverter : IValueConverter
    {
        private readonly double Indentation = 15.0;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var level = -1;
            if (targetType == typeof(double) && typeof(DependencyObject).IsAssignableFrom(value.GetType()))
            {
                var element = value as DependencyObject;
                while ((element = VisualTreeHelper.GetParent(element)) != null)
                {
                    if (typeof(TreeViewItem).IsAssignableFrom(element.GetType()))
                    {
                        level++;
                    }
                }
            }
            return Indentation * level;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
