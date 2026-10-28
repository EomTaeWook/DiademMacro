using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Models;
using Macro.Models.ViewModel;
using Macro.UI;
using System;

namespace Macro.View
{
    public class EventListView : UserControl
    {
        internal Macro.UI.TreeGridView treeGridView;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            treeGridView = this.FindControl<Macro.UI.TreeGridView>("treeGridView");
        }

        private static readonly DataFormat<TreeGridViewItem> _dragFormat = DataFormat.CreateInProcessFormat<TreeGridViewItem>(typeof(TreeGridViewItem).FullName);
        private readonly EventListViewModel _viewModel = new();
        private bool _isDragging;
        private TreeGridViewItem _dragSourceTreeGridViewItem;
        private PointerPressedEventArgs _dragStartEventArgs;
        private readonly ColumnDefinitions _columns;

        public ColumnDefinitions Columns
        {
            get
            {
                return _columns;
            }
        }

        public EventListView()
        {
            InitializeComponent();
            _columns = (ColumnDefinitions)Resources["EventColumns"];
            DataContext = _viewModel;
            treeGridView.AddHandler(PointerPressedEvent, TreeGridView_PointerPressed, RoutingStrategies.Tunnel);
            treeGridView.PointerMoved += TreeGridView_PointerMoved;
            treeGridView.AddHandler(DragDrop.DropEvent, TreeGridView_Drop);
            treeGridView.AddHandler(DragDrop.DragOverEvent, TreeGridView_DragOver);
            SizeChanged += EventListView_SizeChanged;
        }

        private void MoveEventTriggerTreeItem(TreeGridViewItem targetItem, TreeGridViewItem sourceItem)
        {
            if (treeGridView.IsEffectivelyEnabled == false)
            {
                return;
            }
            EventInfoModel eventInfo = sourceItem.DataContext<EventInfoModel>();
            TreeGridViewItem sourceParent = sourceItem.ParentItem;
            EventInfoModel targetEventInfo = null;
            if (targetItem != null)
            {
                targetEventInfo = targetItem.DataContext<EventInfoModel>();
            }
            if (sourceParent == null)
            {
                _viewModel.EventItems.Remove(eventInfo);
            }
            else
            {
                sourceParent.DataContext<EventInfoModel>().SubEventItems.Remove(eventInfo);
            }
            if (targetItem == null)
            {
                _viewModel.EventItems.Add(eventInfo);
            }
            else
            {
                targetEventInfo.SubEventItems.Add(eventInfo);
                targetItem.IsExpanded = true;
            }
            NotifyHelper.InvokeNotify(NotifyEventType.EventTriggerSaved, new EventInfoEventArgs { Index = eventInfo.ItemIndex, EventInfoModel = eventInfo });
        }

        private void TreeGridView_DragOver(object sender, DragEventArgs eventArgs)
        {
            eventArgs.DragEffects = DragDropEffects.None;
            if (treeGridView.IsEffectivelyEnabled == false)
            {
                eventArgs.Handled = true;
                return;
            }
            if (_isDragging == true)
            {
                eventArgs.DragEffects = DragDropEffects.Move;
            }
            eventArgs.Handled = true;
        }

        private void TreeGridView_Drop(object sender, DragEventArgs eventArgs)
        {
            if (treeGridView.IsEffectivelyEnabled == false)
            {
                return;
            }
            if (_isDragging == false)
            {
                return;
            }
            TreeGridViewItem sourceItem = eventArgs.DataTransfer.TryGetValue(_dragFormat);
            if (sourceItem == null)
            {
                return;
            }
            TreeGridViewItem targetItem = treeGridView.TryFindFromPoint<TreeGridViewItem>(eventArgs.GetPosition(treeGridView));
            TreeGridViewItem targetAncestor = targetItem;
            while (targetAncestor != null)
            {
                if (targetAncestor == sourceItem)
                {
                    return;
                }
                targetAncestor = targetAncestor.ParentItem;
            }
            MoveEventTriggerTreeItem(targetItem, sourceItem);
            eventArgs.Handled = true;
        }

        private async void TreeGridView_PointerMoved(object sender, PointerEventArgs eventArgs)
        {
            if (treeGridView.IsEffectivelyEnabled == false)
            {
                return;
            }
            if (_dragSourceTreeGridViewItem == null)
            {
                return;
            }
            if (_isDragging == true)
            {
                return;
            }
            if (eventArgs.GetCurrentPoint(treeGridView).Properties.IsLeftButtonPressed == false)
            {
                return;
            }
            _isDragging = true;
            try
            {
                DataTransferItem item = new();
                item.Set(_dragFormat, _dragSourceTreeGridViewItem);
                using DataTransfer dataTransfer = new();
                dataTransfer.Add(item);
                await DragDrop.DoDragDropAsync(_dragStartEventArgs, dataTransfer, DragDropEffects.Move);
            }
            finally
            {
                _isDragging = false;
                _dragSourceTreeGridViewItem = null;
                _dragStartEventArgs = null;
            }
        }

        private void TreeGridView_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            if (treeGridView.IsEffectivelyEnabled == false)
            {
                return;
            }
            if (_isDragging == true)
            {
                return;
            }
            if (eventArgs.GetCurrentPoint(treeGridView).Properties.IsLeftButtonPressed == false)
            {
                return;
            }
            _dragStartEventArgs = eventArgs;
            _dragSourceTreeGridViewItem = treeGridView.TryFindFromPoint<TreeGridViewItem>(eventArgs.GetPosition(treeGridView));
        }

        private void EventListView_SizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            _viewModel.Height = Bounds.Height;
            _viewModel.Width = Bounds.Width;
        }
    }
}
