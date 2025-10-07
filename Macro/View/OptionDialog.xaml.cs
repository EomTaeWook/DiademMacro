using Macro.Extensions;
using Macro.Models;
using Macro.Models.ViewModel;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// OptionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OptionDialog : Window
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
        private void InitEvent()
        {
            comboEventType.SelectionChanged += ComboEventType_SelectionChanged;
        }

        private void ComboEventType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();
            var currentEventType = viewModel.SelectedEventType;

            if (currentEventType == Infrastructure.EventType.Image)
            {
                checkSameImageDrag.Visibility = Visibility.Visible;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentEventType == Infrastructure.EventType.Mouse)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Visible;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentEventType == Infrastructure.EventType.Keyboard)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Visible;
                relativeToImagePanel.Visibility = Visibility.Collapsed;
            }
            else if (currentEventType == Infrastructure.EventType.RelativeToImage)
            {
                checkSameImageDrag.Visibility = Visibility.Collapsed;
                btnMouseCoordinate.Visibility = Visibility.Collapsed;
                txtKeyboardCmd.Visibility = Visibility.Collapsed;
                relativeToImagePanel.Visibility = Visibility.Visible;
            }
        }

        public void Init(EventTriggerModel eventTriggerModel)
        {
            var viewModel = this.DataContext<OptionDialogViewModel>();

            viewModel.SelectedEventType = eventTriggerModel.EventType;
        }
    }
}
