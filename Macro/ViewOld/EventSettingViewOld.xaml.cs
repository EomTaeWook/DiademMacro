using DataContainer.Generated;
using Dignus.Coroutine;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.ViewModel;
using Macro.UI;
using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Macro.View
{
    /// <summary>
    /// ConfigEventView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EventSettingViewOld : UserControl
    {
        private bool _isDrag;

        private readonly ObservableCollection<KeyValuePair<RepeatType, string>> _repeatItems = new ObservableCollection<KeyValuePair<RepeatType, string>>();
        private EventSettingViewModel _eventConfigViewModelCached;
        private CoroutineHandler _coroutineHandler = new CoroutineHandler();

        private bool isBtnTreeItemPress = false;
        public EventSettingViewOld()
        {
            InitializeComponent();
            InitEvent();

            _isDrag = false;

            _eventConfigViewModelCached = ServiceResolver.GetService<EventSettingViewModel>();
            DataContext = _eventConfigViewModelCached;
        }
        public EventSettingViewModel GetDataContext()
        {
            return _eventConfigViewModelCached;
        }
        public void BindingItems(IEnumerable<EventTriggerModel> items)
        {
            foreach (var item in items)
            {
                _eventConfigViewModelCached.TriggerSaves.Add(item);
            }
        }
        private void RadioButtonRefresh()
        {
            var etm = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();

            if (etm.EventType == EventType.Mouse)
            {
                gridKeyboard.Visibility = Visibility.Collapsed;
                gridRelative.Visibility = Visibility.Collapsed;
                gridMouse.Visibility = Visibility.Visible;
                gridImage.Visibility = Visibility.Collapsed;
                gridHardClick.Visibility = Visibility.Visible;
            }
            else if (etm.EventType == EventType.Keyboard)
            {
                gridKeyboard.Visibility = Visibility.Visible;
                gridMouse.Visibility = Visibility.Collapsed;
                gridRelative.Visibility = Visibility.Collapsed;
                gridImage.Visibility = Visibility.Collapsed;
                gridHardClick.Visibility = Visibility.Collapsed;
            }
            else if (etm.EventType == EventType.RelativeToImage)
            {
                gridRelative.Visibility = Visibility.Visible;
                gridKeyboard.Visibility = Visibility.Collapsed;
                gridMouse.Visibility = Visibility.Collapsed;
                gridImage.Visibility = Visibility.Collapsed;
                gridHardClick.Visibility = Visibility.Visible;
            }
            else if (etm.EventType == EventType.Image)
            {
                gridImage.Visibility = Visibility.Visible;
                gridMouse.Visibility = Visibility.Collapsed;
                gridKeyboard.Visibility = Visibility.Collapsed;
                gridRelative.Visibility = Visibility.Collapsed;
                gridHardClick.Visibility = Visibility.Visible;
            }
            else
            {
                gridRelative.Visibility = Visibility.Collapsed;
                gridKeyboard.Visibility = Visibility.Collapsed;
                gridMouse.Visibility = Visibility.Collapsed;
                gridImage.Visibility = Visibility.Visible;
                gridHardClick.Visibility = Visibility.Visible;
            }
        }
        private void ItemContainerPositionChange(TreeGridViewItem target)
        {
            var parentItemContainer = _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem == null ? this.DataContext<EventSettingViewModel>().TriggerSaves : _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem.DataContext<EventTriggerModel>().SubEventItems;

            if (target != null)
            {
                if (target == _eventConfigViewModelCached.CurrentTreeViewItem)
                {
                    return;
                }

                var item = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
                var targetItem = target.DataContext<EventTriggerModel>();

                if (target.ParentItem == null && _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem == null)
                {
                    parentItemContainer.Remove(item);
                    targetItem.SubEventItems.Add(item);
                }
                else if (target.ParentItem != _eventConfigViewModelCached.CurrentTreeViewItem)
                {
                    parentItemContainer.Remove(item);
                    targetItem.SubEventItems.Add(item);
                }
                else if (target.ParentItem == _eventConfigViewModelCached.CurrentTreeViewItem)
                {
                    parentItemContainer.Remove(item);
                    item.SubEventItems.Remove(targetItem);
                    var targetSubItem = targetItem.SubEventItems;
                    targetItem.SubEventItems = item.SubEventItems;
                    item.SubEventItems = targetSubItem;
                    targetItem.SubEventItems.Add(item);
                    parentItemContainer.Add(targetItem);
                    _eventConfigViewModelCached.Clear();
                }
            }
            else
            {
                var item = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
                parentItemContainer.Remove(item);
                _eventConfigViewModelCached.TriggerSaves.Add(item);
            }
        }
        public EventTriggerModel CopyCurrentItem()
        {
            if (_eventConfigViewModelCached.IsExistence() == false)
            {
                return null;
            }

            Dispatcher.Invoke(() =>
            {
                var selectModel = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
                _eventConfigViewModelCached.CurrentTreeViewItem = new TreeGridViewItem()
                {
                    DataContext = new EventTriggerModel(selectModel)
                };
            });
            return _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
        }
        public void InsertCurrentItem()
        {
            if (_eventConfigViewModelCached.IsExistence() == false)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var treeViewItem = treeSaves.GetSelectItemFromObject<TreeGridViewItem>(_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>());

                if (treeViewItem == null)
                {
                    var model = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
                    _eventConfigViewModelCached.TriggerSaves.Add(model);
                    NotifyHelperOld.InvokeNotify(NotifyEventOldType.EventTriggerInserted, new EventTriggerEventArgs()
                    {
                        Index = model.TriggerIndex,
                        TriggerModel = model
                    });
                }
                Clear();
            });
        }
        public void RemoveCurrentItem()
        {
            if (_eventConfigViewModelCached.IsExistence() == false)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var model = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();

                _eventConfigViewModelCached.CurrentTreeViewItem.IsSelected = false;

                if (_eventConfigViewModelCached.CurrentTreeViewItem.ParentItem == null)
                {
                    _eventConfigViewModelCached.TriggerSaves.Remove(model);
                }
                else
                {
                    _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem.DataContext<EventTriggerModel>().SubEventItems.Remove(model);
                }

                NotifyHelperOld.InvokeNotify(NotifyEventOldType.EventTriggerRemoved, new EventTriggerEventArgs()
                {
                    Index = model.TriggerIndex,
                    TriggerModel = model
                });

                _eventConfigViewModelCached.Clear();
            });
        }

        public void Clear()
        {
            _eventConfigViewModelCached.Clear();

            RadioButtonRefresh();
            btnTreeItemUp.Visibility = btnTreeItemDown.Visibility = Visibility.Hidden;
            lblRepeatSubItems.Visibility = Visibility.Collapsed;
            gridRepeat.Visibility = Visibility.Collapsed;
            btnRemoveROI.Visibility = Visibility.Hidden;
            btnSetROI.Visibility = Visibility.Visible;
        }

        public void RefreshUI()
        {
            RadioButtonRefresh();

            var current = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>();
            btnTreeItemUp.Visibility = btnTreeItemDown.Visibility = Visibility.Visible;
            if (current.SubEventItems.Count != 0)
            {
                lblRepeatSubItems.Visibility = Visibility.Visible;
                gridRepeat.Visibility = Visibility.Visible;
            }
            else
            {
                lblRepeatSubItems.Visibility = Visibility.Collapsed;
                gridRepeat.Visibility = Visibility.Collapsed;
            }
            if (current.RoiData.IsExists() == false)
            {
                this.btnRemoveROI.Visibility = Visibility.Hidden;
                this.btnSetROI.Visibility = Visibility.Visible;
            }
            else
            {
                btnRemoveROI.Visibility = Visibility.Visible;
                btnSetROI.Visibility = Visibility.Hidden;
            }
        }


        private void InitEvent()
        {
            this.Loaded += EventConfigView_Loaded;
            NotifyHelperOld.ScreenCaptureCompleted += NotifyHelper_ScreenCaptureDataBind;
            NotifyHelperOld.MousePositionDataBind += NotifyHelper_MousePositionDataBind;
            NotifyHelperOld.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelperOld.TreeGridViewFocus += NotifyHelper_TreeGridViewFocus;

            var radioButtons = this.FindChildren<RadioButton>();
            foreach (var button in radioButtons)
            {
                button.Click += RadioButton_Click;
            }
            var commonButtons = this.FindChildren<Button>();
            foreach (var button in commonButtons)
            {
                button.Click += Button_Click;
            }

            btnTreeItemUp.PreviewMouseDown += BtnTreeItem_MouseDown;
            btnTreeItemDown.PreviewMouseDown += BtnTreeItem_MouseDown;
            btnTreeItemUp.PreviewMouseUp += BtnTreeItem_MouseUp;
            btnTreeItemDown.PreviewMouseUp += BtnTreeItem_MouseUp;

            treeSaves.SelectedItemChanged += TreeSaves_SelectedItemChanged;
            treeSaves.PreviewMouseLeftButtonDown += TreeSaves_PreviewMouseLeftButtonDown;
            treeSaves.MouseMove += TreeSaves_MouseMove;
            treeSaves.Drop += TreeSaves_Drop;

            comboRepeatSubItem.SelectionChanged += ComboRepeatSubItem_SelectionChanged;
            checkSameImageDrag.Checked += CheckSameImageDrag_Checked;
            checkSameImageDrag.Unchecked += CheckSameImageDrag_Checked;
            KeyDown += ConfigEventView_PreviewKeyDown;
        }

        private void NotifyHelper_UpdatedTime(UpdatedTimeArgs obj)
        {
            Dispatcher.Invoke(() =>
            {
                _coroutineHandler.UpdateCoroutines(obj.DeltaTime);
            });
        }

        private void BtnTreeItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isBtnTreeItemPress = true;
            _coroutineHandler.StopAll();
            _coroutineHandler.Start(1F, ProcessLongClickTreePositionButton(sender));
        }
        private IEnumerator ProcessLongClickTreePositionButton(object sender)
        {
            var itemContainer = _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem == null ? this.DataContext<EventSettingViewModel>().TriggerSaves : _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem.DataContext<EventTriggerModel>().SubEventItems;
            var currentIndex = itemContainer.IndexOf(_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>());
            var changedIndex = currentIndex;
            var addIndex = 0;
            if (sender.Equals(btnTreeItemUp))
            {
                addIndex = -1;
            }
            else if (sender.Equals(btnTreeItemDown))
            {
                addIndex = 1;
            }

            while (isBtnTreeItemPress == true)
            {
                if (addIndex == 0)
                {
                    yield break;
                }
                if (_eventConfigViewModelCached.CurrentTreeViewItem == null)
                {
                    yield break;
                }
                if (changedIndex + addIndex < 0 || changedIndex + addIndex >= itemContainer.Count)
                {
                    break;
                }
                changedIndex += addIndex;
                itemContainer.Swap(currentIndex, changedIndex);
                currentIndex = changedIndex;

                yield return 0.3F;
            }

            NotifyHelperOld.InvokeNotify(NotifyEventOldType.EventTriggerOrderChanged, new EventTriggerOrderChangedEventArgs()
            {
                SelectedTreeViewItem = _eventConfigViewModelCached.CurrentTreeViewItem
            });

            yield break;
        }

        private void BtnTreeItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isBtnTreeItemPress = false;
            _coroutineHandler.StopAll();
        }

        private void EventConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadRepeatItems();

            comboRepeatSubItem.ItemsSource = _repeatItems;
            comboRepeatSubItem.DisplayMemberPath = "Value";
            comboRepeatSubItem.SelectedValuePath = "Key";
        }

        private void NotifyHelper_TreeGridViewFocus(TreeGridViewFocusEventArgs obj)
        {
            this.treeSaves.Focus();
        }

        private void CheckSameImageDrag_Checked(object sender, RoutedEventArgs e)
        {
            if (checkSameImageDrag.IsChecked == true)
            {
                numMaxSameImageCount.Visibility = Visibility.Visible;
            }
            else
            {
                numMaxSameImageCount.Visibility = Visibility.Collapsed;
            }
        }
        private void ReloadRepeatItems()
        {
            _repeatItems.Clear();

            foreach (RepeatType type in Enum.GetValues(typeof(RepeatType)))
            {
                if (type == RepeatType.Max)
                {
                    continue;
                }
                var template = TemplateContainer<LabelTemplate>.Find(type.ToString());

                _repeatItems.Add(new KeyValuePair<RepeatType, string>((RepeatType)type, template.GetString()));
            }
        }
        private void NotifyHelper_ConfigChanged(ConfigEventArgs config)
        {
            ReloadRepeatItems();
        }

        private void ComboRepeatSubItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender.Equals(comboRepeatSubItem) && comboRepeatSubItem.SelectedItem is KeyValuePair<RepeatType, string> item)
            {
                if (item.Key == RepeatType.Count || item.Key == RepeatType.RepeatOnChildEvent)
                {
                    numRepeatCount.Visibility = Visibility.Visible;
                }
                else
                {
                    numRepeatCount.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnMouseCoordinate))
            {
                if (_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.MouseInfoEventType != MouseEventType.None)
                {
                    _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo = new MouseTriggerInfo();
                }
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
                ApplicationManager.Instance.ShowMousePointView();
            }
            else if (sender.Equals(btnTreeItemUp) || sender.Equals(btnTreeItemDown))
            {
                if (_eventConfigViewModelCached.IsExistence() == false)
                {
                    return;
                }

                var itemContainer = _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem == null ? this.DataContext<EventSettingViewModel>().TriggerSaves : _eventConfigViewModelCached.CurrentTreeViewItem.ParentItem.DataContext<EventTriggerModel>().SubEventItems;
                var currentIndex = itemContainer.IndexOf(_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>());
                if (currentIndex > 0 && sender.Equals(btnTreeItemUp))
                {
                    itemContainer.Swap(currentIndex, currentIndex - 1);
                    _eventConfigViewModelCached.CurrentTreeViewItem = treeSaves.GetSelectItemFromObject<TreeGridViewItem>(itemContainer[currentIndex - 1]);

                    NotifyHelperOld.InvokeNotify(NotifyEventOldType.EventTriggerOrderChanged, new EventTriggerOrderChangedEventArgs()
                    {
                        SelectedTreeViewItem = _eventConfigViewModelCached.CurrentTreeViewItem
                    });
                }
                else if (currentIndex < itemContainer.Count - 1 && sender.Equals(btnTreeItemDown))
                {
                    itemContainer.Swap(currentIndex, currentIndex + 1);
                    _eventConfigViewModelCached.CurrentTreeViewItem = treeSaves.GetSelectItemFromObject<TreeGridViewItem>(itemContainer[currentIndex + 1]);

                    NotifyHelperOld.InvokeNotify(NotifyEventOldType.EventTriggerOrderChanged, new EventTriggerOrderChangedEventArgs()
                    {
                        SelectedTreeViewItem = _eventConfigViewModelCached.CurrentTreeViewItem
                    });
                }
            }
            else if (sender.Equals(btnSetROI))
            {
                ApplicationManager.Instance.ShowSetROIView();
            }
            else if (sender.Equals(btnRemoveROI))
            {
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().RoiData = null;
                RefreshUI();
            }
            //else if(sender.Equals(btnMouseWheel))
            //{
            //    lblWheelData.Visibility = Visibility.Visible;
            //    gridWheelData.Visibility = Visibility.Visible;
            //    CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.MouseInfoEventType = MouseEventType.Wheel;
            //}
            //else if(sender.Equals(btnWheelCancel))
            //{
            //    lblWheelData.Visibility = Visibility.Collapsed;
            //    gridWheelData.Visibility = Visibility.Collapsed;
            //    CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo = new MouseTriggerInfo()
            //    {
            //        WheelData = 0,
            //        MouseInfoEventType = MouseEventType.LeftClick,
            //        EndPoint = CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.EndPoint,
            //        MiddlePoint = CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.MiddlePoint,
            //        StartPoint = CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.StartPoint
            //    };
            //}
        }

        private void TreeSaves_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeSaves.SelectedItem is EventTriggerModel == false)
            {
                return;
            }

            _eventConfigViewModelCached.CurrentTreeViewItem = treeSaves.GetSelectItemFromObject<TreeGridViewItem>(treeSaves.SelectedItem);
            NotifyHelperOld.InvokeNotify(NotifyEventOldType.SelctTreeViewItemChanged, new SelctTreeViewItemChangedEventArgs()
            {
                TreeViewItem = _eventConfigViewModelCached.CurrentTreeViewItem
            });

            var eventType = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().EventType;

            if (eventType == EventType.Keyboard)
            {
                RadioButton_Click(rbKeyboard, null);
            }
            else if (eventType == EventType.Mouse)
            {
                RadioButton_Click(rbMouse, null);
            }
            else if (eventType == EventType.Image)
            {
                RadioButton_Click(rbImage, null);
            }
            else if (eventType == EventType.RelativeToImage)
            {
                RadioButton_Click(rbRelativeToImage, null);
            }
            RefreshUI();
        }

        private void TreeSaves_Drop(object sender, DragEventArgs e)
        {
            if (_isDrag == true)
            {
                _isDrag = false;
                var targetRow = treeSaves.TryFindFromPoint<TreeGridViewItem>(e.GetPosition(treeSaves));
                if (_eventConfigViewModelCached.CurrentTreeViewItem == targetRow)
                {
                    return;
                }
                ItemContainerPositionChange(targetRow);
                Clear();

                NotifyHelperOld.InvokeNotify(NotifyEventOldType.TreeItemOrderChanged, new EventTriggerOrderChangedEventArgs()
                {
                    SelectedTreeViewItem = targetRow
                });
            }
        }
        private void TreeSaves_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrag && e.LeftButton == MouseButtonState.Pressed)
            {
                var target = (sender as UIElement).TryFindFromPoint<TreeGridViewItem>(e.GetPosition(treeSaves));
                if (target == null)
                    return;

                _isDrag = true;
                _eventConfigViewModelCached.CurrentTreeViewItem = target;

                DragDrop.DoDragDrop(_eventConfigViewModelCached.CurrentTreeViewItem, new object(), DragDropEffects.Move);
            }
        }

        private void TreeSaves_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrag = false;
        }
        private void ConfigEventView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Clear();
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.SelctTreeViewItemChanged, new SelctTreeViewItemChangedEventArgs());
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        private void NotifyHelper_MousePositionDataBind(MousePointEventArgs e)
        {
            _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MonitorInfo = e.MonitorInfo;
            _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo = e.MouseTriggerInfo;

            ApplicationManager.Instance.CloseMousePointView();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void NotifyHelper_ScreenCaptureDataBind(CaptureCompletedEventArgs e)
        {
            RadioButtonRefresh();
        }
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(rbMouse))
            {
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().EventType = EventType.Mouse;
            }
            else if (sender.Equals(rbKeyboard))
            {
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().EventType = EventType.Keyboard;
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().HardClick = false;
            }
            else if (sender.Equals(rbImage))
            {
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().EventType = EventType.Image;
                if (_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.MouseInfoEventType != MouseEventType.None)
                {
                    _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo = new MouseTriggerInfo();
                }
            }
            else if (sender.Equals(rbRelativeToImage))
            {
                _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().EventType = EventType.RelativeToImage;
                if (_eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.MouseInfoEventType != MouseEventType.None)
                {
                    _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo = new MouseTriggerInfo();
                }
                _eventConfigViewModelCached.RelativePosition.X = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.StartPoint.X;
                _eventConfigViewModelCached.RelativePosition.Y = _eventConfigViewModelCached.CurrentTreeViewItem.DataContext<EventTriggerModel>().MouseTriggerInfo.StartPoint.Y;
            }
            RadioButtonRefresh();
        }
    }
}
