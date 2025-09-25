using Macro.Models.ViewModel;
using System.Windows.Controls;

namespace Macro.View
{
    /// <summary>
    /// ConfigEventView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EventListView : UserControl
    {
        private readonly EventListViewModel _viewModel = new EventListViewModel();
        public EventListView()
        {
            InitializeComponent();
            this.Loaded += EventListView_Loaded;
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
