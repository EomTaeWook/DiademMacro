using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Models;
using Macro.Models.ViewModel;
using Macro.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Macro.View
{
    /// <summary>
    /// ConfigEventView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EventListView : UserControl
    {
        private readonly EventListViewModel _viewModel = new EventListViewModel();
        private bool _isDrag;
        public EventListView()
        {
            InitializeComponent();
            this.Loaded += EventListView_Loaded;
            this.treeGridView.PreviewMouseLeftButtonDown += TreeGridView_PreviewMouseLeftButtonDown;
            this.treeGridView.MouseMove += TreeGridView_MouseMove;
            this.treeGridView.Drop += TreeGridView_Drop;
        }
        private void MoveEventTriggerTreeItem(TreeGridViewItem targetTreeGridViewItem, TreeGridViewItem sourceTreeGridViewItem)
        {
            var currentEventModel = sourceTreeGridViewItem.DataContext<EventTriggerModel>();

            if (targetTreeGridViewItem == null)
            {
                if (sourceTreeGridViewItem.ParentItem == null)
                {
                    _viewModel.EventItems.Remove(currentEventModel);
                    _viewModel.EventItems.Add(currentEventModel);
                    return;
                }
                var parentEventModel = sourceTreeGridViewItem.ParentItem.DataContext<EventTriggerModel>();
                parentEventModel.SubEventItems.Remove(currentEventModel);
                _viewModel.EventItems.Add(currentEventModel);
            }
            else if (sourceTreeGridViewItem.ParentItem == null)
            {
                var targetEventModel = targetTreeGridViewItem.DataContext<EventTriggerModel>();
                _viewModel.EventItems.Remove(currentEventModel);
                targetEventModel.SubEventItems.Add(currentEventModel);
            }
            else if (sourceTreeGridViewItem.ParentItem != null)
            {
                var targetEventModel = targetTreeGridViewItem.DataContext<EventTriggerModel>();
                var parentEventModel = sourceTreeGridViewItem.ParentItem.DataContext<EventTriggerModel>();
                parentEventModel.SubEventItems.Remove(currentEventModel);
                targetEventModel.SubEventItems.Add(currentEventModel);
            }
        }
        private void TreeGridView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (_isDrag == true)
            {
                _isDrag = false;

                var targetRow = treeGridView.TryFindFromPoint<TreeGridViewItem>(e.GetPosition(treeGridView));

                if (!(e.Data.GetData(typeof(TreeGridViewItem)) is TreeGridViewItem sourceTreeGridViewItem))
                {
                    return;
                }

                if (targetRow == sourceTreeGridViewItem)
                {
                    return;
                }

                var eventItem = sourceTreeGridViewItem.DataContext<EventTriggerModel>();

                if (eventItem == null)
                {
                    return;
                }

                MoveEventTriggerTreeItem(targetRow, sourceTreeGridViewItem);

                NotifyHelper.InvokeNotify(NotifyEventType.EventTriggerSaved, new EventTriggerEventArgs()
                {
                    Index = eventItem.TriggerIndex,
                    TriggerModel = eventItem
                });
            }
        }

        private void TreeGridView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDrag && e.LeftButton == MouseButtonState.Pressed)
            {
                var target = (sender as UIElement).TryFindFromPoint<TreeGridViewItem>(e.GetPosition(treeGridView));

                if (target == null)
                {
                    return;
                }
                _isDrag = true;

                DragDrop.DoDragDrop(target, target, DragDropEffects.Move);
            }
        }

        private void TreeGridView_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDrag = false;
        }

        private void EventListView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            _viewModel.Height = this.ActualHeight;
            _viewModel.Width = this.ActualWidth;
        }

        private void EventListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.SizeChanged += EventListView_SizeChanged;
            this.EventListView_SizeChanged(this, null);
            DataContext = _viewModel;
        }
    }
}
