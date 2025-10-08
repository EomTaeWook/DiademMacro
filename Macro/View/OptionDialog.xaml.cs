using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
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
        public OptionDialog()
        {
            InitializeComponent();
            DataContext = new OptionDialogViewModel();

            this.Loaded += OptionDialog_Loaded;
        }

        private void OptionDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InitEvent();
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
            LoadRepeatItems();

            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.SelectedEventType = eventTriggerModel.EventType;
            viewModel.SelectedRepeatType = eventTriggerModel.RepeatInfo.RepeatType;

            if (eventTriggerModel.SubEventItems.Count > 0)
            {
                SetRepeatSectionVisibility(true);
            }
            else
            {
                SetRepeatSectionVisibility(false);
            }
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
                checkSameImageDrag.Visibility = Visibility.Visible;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentType == Infrastructure.EventType.Mouse)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Visible;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentType == Infrastructure.EventType.Keyboard)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Visible;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentType == Infrastructure.EventType.RelativeToImage)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Visible;
            }
        }


    }
}
