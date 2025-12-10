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
using Utils.Infrastructure;

namespace Macro.View
{
    /// <summary>
    /// OptionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OptionDialog : MetroWindow
    {
        private EventInfoModel _eventInfoModel;
        public OptionDialog()
        {
            InitializeComponent();
            DataContext = new OptionDialogViewModel();

            this.Loaded += OptionDialog_Loaded;
        }

        private void OptionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InitEvent();

            SetRepeatSectionVisibility(_eventInfoModel.SubEventItems.Count > 0);
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
        public void BindItem(EventInfoModel eventTriggerModel)
        {
            _eventInfoModel = eventTriggerModel;
            LoadRepeatItems();

            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.SelectedEventType = _eventInfoModel.EventType;
            viewModel.MouseEventInfo = _eventInfoModel.MouseEventInfo;
            viewModel.KeyboardCmd = _eventInfoModel.KeyboardCmd;
            viewModel.AfterDelay = _eventInfoModel.AfterDelay;

            viewModel.SelectedRepeatType = eventTriggerModel.RepeatInfo.RepeatType;
            viewModel.RepeatCount = _eventInfoModel.RepeatInfo.Count;
            viewModel.EventToNext = _eventInfoModel.EventToNext;
            viewModel.SameImageDrag = _eventInfoModel.SameImageDrag;
            viewModel.MaxDragCount = _eventInfoModel.MaxDragCount;
            viewModel.HardClick = _eventInfoModel.HardClick;
            viewModel.RoiDataInfo = _eventInfoModel.RoiDataInfo;

            viewModel.PositionRelativeToImageX = _eventInfoModel.PositionRelativeToImage.X;
            viewModel.PositionRelativeToImageY = _eventInfoModel.PositionRelativeToImage.Y;
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

            btnMouseEvent.Click += BtnMouseEvent_Click;

            btnSetRoi.Click += BtnSetRoi_Click;
            btnRemoveRoi.Click += BtnRemoveRoi_Click;
            btnSave.Click += BtnSave_Click;

            NotifyHelper.ROICaptureCompleted += NotifyHelper_ROICaptureCompleted;
            NotifyHelper.MouseInteractionCaptured += NotifyHelper_MouseInteractionCaptured;
        }

        private void NotifyHelper_MouseInteractionCaptured(MouseInteractionCapturedEventArgs obj)
        {
            ApplicationManager.Instance.CloseMousePositionViews();
            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.MouseEventInfo = obj.MouseEventInfo;
        }

        private void BtnMouseEvent_Click(object sender, RoutedEventArgs e)
        {
            ApplicationManager.Instance.ShowAndActivateMousePositionViews();
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
            if (_eventInfoModel == null)
            {
                return;
            }
            var viewModel = this.DataContext<OptionDialogViewModel>();

            _eventInfoModel.EventType = viewModel.SelectedEventType;
            _eventInfoModel.MouseEventInfo = viewModel.MouseEventInfo;
            _eventInfoModel.KeyboardCmd = viewModel.KeyboardCmd;
            _eventInfoModel.AfterDelay = viewModel.AfterDelay;
            _eventInfoModel.RepeatInfo = new RepeatInfoModel()
            {
                Count = viewModel.RepeatCount,
                RepeatType = viewModel.SelectedRepeatType
            };
            _eventInfoModel.EventToNext = viewModel.EventToNext;
            _eventInfoModel.SameImageDrag = viewModel.SameImageDrag;
            _eventInfoModel.MaxDragCount = viewModel.MaxDragCount;
            _eventInfoModel.HardClick = viewModel.HardClick;
            _eventInfoModel.RoiDataInfo = viewModel.RoiDataInfo;

            _eventInfoModel.PositionRelativeToImage = new Point2D(viewModel.PositionRelativeToImageX, viewModel.PositionRelativeToImageY);

            NotifyHelper.InvokeNotify(NotifyEventType.EventTriggerSaved, new EventInfoEventArgs()
            {
                Index = _eventInfoModel.ItemIndex,
                EventInfoModel = _eventInfoModel
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
            ApplicationManager.Instance.ShowSetROIViews();
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
            numMaxDragCount.IsEnabled = false;

            if (currentType == Infrastructure.EventType.Image)
            {
                checkSameImageDrag.IsEnabled = true;

                panelMouseEvent.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
                checkHardClick.IsChecked = false;
                checkHardClick.IsEnabled = false;
            }
            else if (currentType == Infrastructure.EventType.Mouse)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                panelMouseEvent.Visibility = Visibility.Visible;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
                checkHardClick.IsEnabled = true;
            }
            else if (currentType == Infrastructure.EventType.Keyboard)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
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
                panelMouseEvent.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Visible;
                checkHardClick.IsEnabled = true;
            }
        }
    }
}
