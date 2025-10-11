using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// OptionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OptionDialog : MetroWindow
    {
        private EventTriggerModel _eventTriggerModel;
        public OptionDialog()
        {
            InitializeComponent();
            DataContext = new OptionDialogViewModel();

            this.Loaded += OptionDialog_Loaded;
        }

        private void OptionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InitEvent();

            SetRepeatSectionVisibility(_eventTriggerModel.SubEventItems.Count > 0);
            ComboEventType_SelectionChanged(comboEventType, null);
            CheckSameImageDrag_ValueChanged(checkSameImageDrag, null);
        }
        private void LoadRepeatItems()
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            viewModel.RepeatItemsSource.Clear();
            foreach (RepeatType type in Enum.GetValues(typeof(RepeatType)))
            {
                if (type == RepeatType.Max)
                {
                    continue;
                }
                var template = TemplateContainer<LabelTemplate>.Find(type.ToString());
                viewModel.RepeatItemsSource.Add(new KeyValuePair<RepeatType, string>((RepeatType)type, template.GetString()));
            }
        }
        public void BindItem(EventTriggerModel eventTriggerModel)
        {
            _eventTriggerModel = eventTriggerModel;
            LoadRepeatItems();

            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.SelectedEventType = _eventTriggerModel.EventType;
            viewModel.MouseEventInfo = _eventTriggerModel.MouseEventInfo;
            viewModel.KeyboardCmd = _eventTriggerModel.KeyboardCmd;
            viewModel.AfterDelay = _eventTriggerModel.AfterDelay;

            viewModel.SelectedRepeatType = eventTriggerModel.RepeatInfo.RepeatType;
            viewModel.RepeatCount = _eventTriggerModel.RepeatInfo.Count;
            viewModel.EventToNext = _eventTriggerModel.EventToNext;
            viewModel.SameImageDrag = _eventTriggerModel.SameImageDrag;
            viewModel.MaxDragCount = _eventTriggerModel.MaxDragCount;
            viewModel.HardClick = _eventTriggerModel.HardClick;
            viewModel.RoiDataInfo = _eventTriggerModel.RoiDataInfo;
            viewModel.RelativeX = (int)_eventTriggerModel.MouseEventInfo?.StartPoint.X;
            viewModel.RelativeY = (int)_eventTriggerModel.MouseEventInfo?.StartPoint.Y;
        }
        private void SetRepeatSectionVisibility(bool isVisible)
        {
            if (isVisible == true)
            {
                comboRepeatItem.Visibility = Visibility.Visible;
                labelRepeatItem.Visibility = Visibility.Visible;
                numRepeatCount.Visibility = Visibility.Visible;
            }
            else
            {
                comboRepeatItem.Visibility = Visibility.Collapsed;
                labelRepeatItem.Visibility = Visibility.Collapsed;
                numRepeatCount.Visibility = Visibility.Collapsed;
            }
        }
        private void InitEvent()
        {
            comboEventType.SelectionChanged += ComboEventType_SelectionChanged;
            comboRepeatItem.SelectionChanged += ComboRepeatItem_SelectionChanged;
            checkSameImageDrag.Checked += CheckSameImageDrag_ValueChanged;
            checkSameImageDrag.Unchecked += CheckSameImageDrag_ValueChanged;

            btnSetRoi.Click += BtnSetRoi_Click;
            btnRemoveRoi.Click += BtnRemoveRoi_Click;
            btnSave.Click += BtnSave_Click;

            NotifyHelper.ROICaptureCompleted += NotifyHelper_ROICaptureCompleted;
        }

        private void NotifyHelper_ROICaptureCompleted(ROICaptureCompletedEventArgs obj)
        {
            ApplicationManager.Instance.CloseCaptureView();
            var viewModel = this.DataContext<OptionDialogViewModel>();
            viewModel.RoiDataInfo = new RoiModel()
            {
                MonitorInfo = obj.MonitorInfo,
                RoiRect = obj.RoiRect,
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_eventTriggerModel == null)
            {
                return;
            }
            var viewModel = this.DataContext<OptionDialogViewModel>();

            _eventTriggerModel.EventType = viewModel.SelectedEventType;
            _eventTriggerModel.MouseEventInfo = viewModel.MouseEventInfo;
            _eventTriggerModel.KeyboardCmd = viewModel.KeyboardCmd;
            _eventTriggerModel.AfterDelay = viewModel.AfterDelay;
            _eventTriggerModel.RepeatInfo = new RepeatInfoModel()
            {
                Count = viewModel.RepeatCount,
                RepeatType = viewModel.SelectedRepeatType
            };
            _eventTriggerModel.EventToNext = viewModel.EventToNext;
            _eventTriggerModel.SameImageDrag = viewModel.SameImageDrag;
            _eventTriggerModel.MaxDragCount = viewModel.MaxDragCount;
            _eventTriggerModel.HardClick = viewModel.HardClick;
            _eventTriggerModel.RoiDataInfo = viewModel.RoiDataInfo;
            _eventTriggerModel.MouseEventInfo.StartPoint = new Point()
            {
                X = viewModel.RelativeX,
                Y = viewModel.RelativeY,
            };
            NotifyHelper.InvokeNotify(NotifyEventType.EventTriggerSaved, new EventTriggerEventArgs()
            {
                Index = _eventTriggerModel.TriggerIndex,
                TriggerModel = _eventTriggerModel
            });
            this.Close();
        }

        private void BtnRemoveRoi_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            viewModel.RoiDataInfo = new RoiModel();
        }
        private void BtnSetRoi_Click(object sender, RoutedEventArgs e)
        {
            ApplicationManager.Instance.ShowSetROIView();
        }
        private void CheckSameImageDrag_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (checkSameImageDrag.IsChecked == true)
            {
                numMaxDragCount.IsEnabled = true;
            }
            else
            {
                numMaxDragCount.Value = 0;
                numMaxDragCount.IsEnabled = false;
            }
        }

        private void ComboRepeatItem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            var currentType = viewModel.SelectedRepeatType;

            if (currentType == RepeatType.RepeatOnChildEvent)
            {
                numRepeatCount.Visibility = Visibility.Collapsed;
            }
            else
            {
                numRepeatCount.Visibility = Visibility.Visible;
            }
        }

        private void ComboEventType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            var currentType = viewModel.SelectedEventType;

            if (currentType == Infrastructure.EventType.Image)
            {
                checkSameImageDrag.IsEnabled = true;
                numMaxDragCount.IsEnabled = true;
                panelMouseEvent.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
                checkHardClick.IsEnabled = true;
            }
            else if (currentType == Infrastructure.EventType.Mouse)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                numMaxDragCount.IsEnabled = false;
                panelMouseEvent.Visibility = Visibility.Visible;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
                checkHardClick.IsEnabled = true;
            }
            else if (currentType == Infrastructure.EventType.Keyboard)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                numMaxDragCount.IsEnabled = false;
                panelMouseEvent.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Visible;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
                checkHardClick.IsChecked = false;
                checkHardClick.IsEnabled = false;
            }
            else if (currentType == Infrastructure.EventType.RelativeToImage)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                numMaxDragCount.IsEnabled = false;
                panelMouseEvent.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Visible;
                checkHardClick.IsEnabled = true;
            }
        }
    }
}
