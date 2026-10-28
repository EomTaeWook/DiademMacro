using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.ViewModel;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Utils.Infrastructure;

namespace Macro.View
{
    /// <summary>
    /// OptionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public class OptionDialog : Window
    {
        internal Avalonia.Controls.Grid gridMain;
        internal Avalonia.Controls.ComboBox comboEventType;
        internal Avalonia.Controls.StackPanel panelMouseEvent;
        internal Avalonia.Controls.Button btnMouseEvent;
        internal Avalonia.Controls.Label MousePointDesc;
        internal Avalonia.Controls.TextBox txtKeyboardCmd;
        internal Avalonia.Controls.StackPanel relativeToImagePanel;
        internal Avalonia.Controls.NumericUpDown numRelativeX;
        internal Avalonia.Controls.NumericUpDown numRelativeY;
        internal Avalonia.Controls.NumericUpDown numEventToNext;
        internal Avalonia.Controls.NumericUpDown numAfterDelay;
        internal Avalonia.Controls.NumericUpDown numSimilarity;
        internal Avalonia.Controls.Button btnSetRoi;
        internal Avalonia.Controls.Button btnRemoveRoi;
        internal Avalonia.Controls.Label labelROIDesc;
        internal Avalonia.Controls.CheckBox checkHardClick;
        internal Avalonia.Controls.CheckBox checkSameImageDrag;
        internal Avalonia.Controls.NumericUpDown numMaxDragCount;
        internal Avalonia.Controls.Label labelRepeatItem;
        internal Avalonia.Controls.ComboBox comboRepeatItem;
        internal Avalonia.Controls.NumericUpDown numRepeatCount;
        internal Avalonia.Controls.Button btnSave;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            gridMain = this.FindControl<Avalonia.Controls.Grid>("gridMain");
            comboEventType = this.FindControl<Avalonia.Controls.ComboBox>("comboEventType");
            panelMouseEvent = this.FindControl<Avalonia.Controls.StackPanel>("panelMouseEvent");
            btnMouseEvent = this.FindControl<Avalonia.Controls.Button>("btnMouseEvent");
            MousePointDesc = this.FindControl<Avalonia.Controls.Label>("MousePointDesc");
            txtKeyboardCmd = this.FindControl<Avalonia.Controls.TextBox>("txtKeyboardCmd");
            relativeToImagePanel = this.FindControl<Avalonia.Controls.StackPanel>("relativeToImagePanel");
            numRelativeX = this.FindControl<Avalonia.Controls.NumericUpDown>("numRelativeX");
            numRelativeY = this.FindControl<Avalonia.Controls.NumericUpDown>("numRelativeY");
            numEventToNext = this.FindControl<Avalonia.Controls.NumericUpDown>("numEventToNext");
            numAfterDelay = this.FindControl<Avalonia.Controls.NumericUpDown>("numAfterDelay");
            numSimilarity = this.FindControl<Avalonia.Controls.NumericUpDown>("numSimilarity");
            btnSetRoi = this.FindControl<Avalonia.Controls.Button>("btnSetRoi");
            btnRemoveRoi = this.FindControl<Avalonia.Controls.Button>("btnRemoveRoi");
            labelROIDesc = this.FindControl<Avalonia.Controls.Label>("labelROIDesc");
            checkHardClick = this.FindControl<Avalonia.Controls.CheckBox>("checkHardClick");
            checkSameImageDrag = this.FindControl<Avalonia.Controls.CheckBox>("checkSameImageDrag");
            numMaxDragCount = this.FindControl<Avalonia.Controls.NumericUpDown>("numMaxDragCount");
            labelRepeatItem = this.FindControl<Avalonia.Controls.Label>("labelRepeatItem");
            comboRepeatItem = this.FindControl<Avalonia.Controls.ComboBox>("comboRepeatItem");
            numRepeatCount = this.FindControl<Avalonia.Controls.NumericUpDown>("numRepeatCount");
            btnSave = this.FindControl<Avalonia.Controls.Button>("btnSave");
        }

        private EventInfoModel _eventInfoModel;
        public OptionDialog()
        {
            InitializeComponent();
            DataContext = new OptionDialogViewModel();

            Opened += OptionDialog_Loaded;
            Closed += OptionDialog_Closed;
        }

        private void OptionDialog_Loaded(object sender, EventArgs e)
        {
            InitEvent();

            SetRepeatSectionVisibility(_eventInfoModel.SubEventItems.Count > 0);
            ComboEventType_SelectionChanged(comboEventType, null);
            CheckSameImageDrag_ValueChanged(checkSameImageDrag, null);
        }
        private void OptionDialog_Closed(object sender, EventArgs eventArgs)
        {
            UIManager.Instance.ButtonActionStateChanged -= UpdateButtonState;
            NotifyHelper.ROICaptureCompleted -= NotifyHelper_ROICaptureCompleted;
            NotifyHelper.MouseInteractionCaptured -= NotifyHelper_MouseInteractionCaptured;
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
            viewModel.Similarity = _eventInfoModel.Similarity;

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
                comboRepeatItem.IsVisible = true;
                labelRepeatItem.IsVisible = true;
                numRepeatCount.IsVisible = true;
            }
            else
            {
                comboRepeatItem.IsVisible = false;
                labelRepeatItem.IsVisible = false;
                numRepeatCount.IsVisible = false;
            }
        }
        private void InitEvent()
        {
            UIManager.Instance.ButtonActionStateChanged += UpdateButtonState;
            UpdateButtonState();
            comboEventType.SelectionChanged += ComboEventType_SelectionChanged;
            comboRepeatItem.SelectionChanged += ComboRepeatItem_SelectionChanged;
            checkSameImageDrag.IsCheckedChanged += CheckSameImageDrag_ValueChanged;

            btnMouseEvent.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnMouseEvent_Click(sender, eventArgs));

            btnSetRoi.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnSetRoi_Click(sender, eventArgs));
            btnRemoveRoi.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnRemoveRoi_Click(sender, eventArgs));
            btnSave.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnSave_Click(sender, eventArgs));

            NotifyHelper.ROICaptureCompleted += NotifyHelper_ROICaptureCompleted;
            NotifyHelper.MouseInteractionCaptured += NotifyHelper_MouseInteractionCaptured;
        }

        private void UpdateButtonState()
        {
            gridMain.IsEnabled = UIManager.Instance.IsProcessingButtonActions == false;
        }

        private void NotifyHelper_MouseInteractionCaptured(MouseInteractionCapturedEventArgs obj)
        {
            ApplicationManager.Instance.CloseMousePositionViews();
            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.MouseEventInfo = obj.MouseEventInfo;
        }

        private void BtnMouseEvent_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }
            if (ApplicationManager.Instance.HasActiveInputOverlay == true)
            {
                return;
            }
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
            if (IsVisible == false)
            {
                return;
            }
            if (ApplicationManager.Instance.HasActiveInputOverlay == true)
            {
                return;
            }
            if (_eventInfoModel == null)
            {
                return;
            }
            var viewModel = this.DataContext<OptionDialogViewModel>();

            _eventInfoModel.EventType = viewModel.SelectedEventType;
            _eventInfoModel.MouseEventInfo = viewModel.MouseEventInfo;
            _eventInfoModel.KeyboardCmd = viewModel.KeyboardCmd;
            _eventInfoModel.AfterDelay = viewModel.AfterDelay;
            _eventInfoModel.Similarity = viewModel.Similarity;
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
            if (IsVisible == false)
            {
                return;
            }
            if (ApplicationManager.Instance.HasActiveInputOverlay == true)
            {
                return;
            }
            var viewModel = this.DataContext<OptionDialogViewModel>();
            viewModel.RoiDataInfo = new RoiModel();
        }
        private void BtnSetRoi_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }
            if (ApplicationManager.Instance.HasActiveInputOverlay == true)
            {
                return;
            }
            ApplicationManager.Instance.ShowSetROIViews();
        }
        private void CheckSameImageDrag_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (checkSameImageDrag.IsChecked == true)
            {
                numMaxDragCount.IsEnabled = true;
                if (this.DataContext<OptionDialogViewModel>().SelectedEventType == EventType.Image)
                {
                    checkHardClick.IsChecked = false;
                    checkHardClick.IsEnabled = false;
                }
            }
            else
            {
                numMaxDragCount.Value = 0;
                numMaxDragCount.IsEnabled = false;
                if (this.DataContext<OptionDialogViewModel>().SelectedEventType == EventType.Image)
                {
                    checkHardClick.IsEnabled = true;
                }
            }
        }

        private void ComboRepeatItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            var currentType = viewModel.SelectedRepeatType;

            if (currentType == RepeatType.RepeatOnChildEvent)
            {
                numRepeatCount.IsVisible = false;
            }
            else
            {
                numRepeatCount.IsVisible = true;
            }
        }

        private void ComboEventType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            var currentType = viewModel.SelectedEventType;
            numMaxDragCount.IsEnabled = false;

            if (currentType == EventType.Image)
            {
                checkSameImageDrag.IsEnabled = true;

                panelMouseEvent.IsVisible = false;
                txtKeyboardCmd.IsVisible = false;
                relativeToImagePanel.IsVisible = false;
                checkHardClick.IsEnabled = checkSameImageDrag.IsChecked == false;
                if (checkSameImageDrag.IsChecked == true)
                {
                    checkHardClick.IsChecked = false;
                }
            }
            else if (currentType == EventType.Mouse)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                panelMouseEvent.IsVisible = true;
                txtKeyboardCmd.IsVisible = false;
                relativeToImagePanel.IsVisible = false;
                checkHardClick.IsEnabled = true;
            }
            else if (currentType == EventType.Keyboard)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                panelMouseEvent.IsVisible = false;
                txtKeyboardCmd.IsVisible = true;
                relativeToImagePanel.IsVisible = false;
                checkHardClick.IsChecked = false;
                checkHardClick.IsEnabled = false;
            }
            else if (currentType == EventType.RelativeToImage)
            {
                checkSameImageDrag.IsChecked = false;
                checkSameImageDrag.IsEnabled = false;
                panelMouseEvent.IsVisible = false;
                txtKeyboardCmd.IsVisible = false;
                relativeToImagePanel.IsVisible = true;
                checkHardClick.IsEnabled = true;
            }
        }
    }
}
